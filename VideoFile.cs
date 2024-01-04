﻿using System;
using System.IO;
using System.Xml.Linq;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Generic;
using System.Threading;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ImageMagick;
using System.Reflection.Metadata;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using System.Linq;
using SolrNet;
using System.Text;

namespace MediaCurator
{
   class VideoFile : MediaFile
   {
      #region Constants

      private Lazy<Dictionary<string, Dictionary<string, int>>> ThumbnailsConfiguration => new(() =>
      {
         var section = Configuration.GetSection("Thumbnails:Video");

         if (section.Exists())
         {
            return section.Get<Dictionary<string, Dictionary<string, int>>>();
         }

         return new Dictionary<string, Dictionary<string, int>>();
      });

      #endregion // Constants

      #region Fields

      private double _duration = -1.0;

      /// <summary>
      /// Gets or sets the duration of the video file.
      /// </summary>
      /// <value>
      /// The video file duration.
      /// </value>
      public double Duration
      {
         get => _duration;

         set
         {
            if (_duration != value)
            {
               Modified = true;

               _duration = value;
            }
         }
      }

      private long _width  = -1;
      private long _height = -1;

      /// <summary>
      /// Gets or sets the resolution of the video file.
      /// </summary>
      /// <value>
      /// The video file resolution.
      /// </value>
      public ResolutionType Resolution
      {
         get => new(_width, _height);

         set
         {
            if (Resolution != value)
            {
               Modified = true;

               _width = value.Width;
               _height = value.Height;
            }
         }
      }

      /// <summary>
      /// Gets a tailored MediaContainer model describing a video file.
      /// </summary>
      public override Models.MediaContainer Model
      {
         get
         {
            var model = base.Model;

            model.Duration = Duration;
            model.Width = Resolution.Width;
            model.Height = Resolution.Height;

            return model;
         }

         set
         {
            if (value == null) return;

            base.Model = value;

            Duration = value.Duration;
            Resolution = new(value.Width, value.Height);
         }
      }

      #endregion // Fields

      #region Constructors

      public VideoFile(ILogger<MediaContainer> logger,
                       IServiceProvider services,
                       IConfiguration configuration,
                       IThumbnailsDatabase thumbnailsDatabase,
                       IMediaLibrary mediaLibrary,
                       string id = null, string path = null
      ) : base(logger, services, configuration, thumbnailsDatabase, mediaLibrary, id, path)
      {
         // The base class constructor will take care of the entry, its general attributes and its
         // parents and below we'll take care of its specific attributes.

         if (Skipped) return;
      }

      #endregion // Constructors

      #region Video File Operations

      public override void GetFileInfo(string path)
      {
         string output = null;
         string executable = Configuration["FFmpeg:Path"] + Platform.Separator.Path + "ffprobe" + Platform.Extension.Executable;

         if (!File.Exists(executable))
         {
            throw new DirectoryNotFoundException("ffprobe not found at the specified path: " + executable);
         }

         using (Process ffprobe = new Process())
         {
            ffprobe.StartInfo.FileName = executable;
            ffprobe.StartInfo.Arguments = "-v quiet -print_format json -show_format ";
            ffprobe.StartInfo.Arguments += "-show_streams -select_streams v:0 " + "\"" + path + "\"";
            ffprobe.StartInfo.CreateNoWindow = true;
            ffprobe.StartInfo.UseShellExecute = false;
            ffprobe.StartInfo.RedirectStandardOutput = true;

            ffprobe.Start();

            output = ffprobe.StandardOutput.ReadToEnd();

            ffprobe.WaitForExit(Configuration.GetSection("FFmpeg:Timeout").Get<Int32>());
         }

         var fileInfo = JsonDocument.Parse(output).RootElement;

         /*--------------------------------------------------------------------------------
                                           DURATION
         --------------------------------------------------------------------------------*/

         try
         {
            Duration = Double.Parse(fileInfo.GetProperty("format").GetProperty("duration").GetString(),
                                    CultureInfo.InvariantCulture);
         }
         catch (Exception e)
         {
            Logger.LogDebug("Failed To Retrieve Duration For: {}, Because: {}", FullPath, e.Message);
         }

         /*--------------------------------------------------------------------------------
                                           RESOLUTION
         --------------------------------------------------------------------------------*/

         try
         {
            Resolution = new ResolutionType(String.Format("{0}x{1}",
                                            fileInfo.GetProperty("streams")[0].GetProperty("width").GetUInt32(),
                                            fileInfo.GetProperty("streams")[0].GetProperty("height").GetUInt32()));
         }
         catch (Exception e)
         {
            Logger.LogDebug("Failed To Retrieve Resolution For: {}, Because: {}", FullPath, e.Message);
         }
      }

      private byte[] GenerateThumbnail(string path, int position, int width, int height, bool crop)
      {
         byte[] output = null;
         string executable = Configuration["FFmpeg:Path"] + Platform.Separator.Path + "ffmpeg" + Platform.Extension.Executable;

         int waitInterval = 100;
         int totalWaitTime = 0;

         if (!File.Exists(executable))
         {
            throw new DirectoryNotFoundException("ffmpeg not found at the specified path: " + executable);
         }

         using (Process ffmpeg = new())
         {
            ffmpeg.StartInfo.FileName = executable;

            ffmpeg.StartInfo.Arguments = String.Format("-ss {0} -i \"{1}\" ", position.ToString(), path);
            ffmpeg.StartInfo.Arguments += String.Format("-y -vf select=\"eq(pict_type\\,I),scale={0}:{1}", width.ToString(), height.ToString());

            if (crop)
            {
               ffmpeg.StartInfo.Arguments += String.Format(",crop=iw:'min({0},ih)'", (height > 0) ? height.ToString() : "iw/16*9");
            }

            ffmpeg.StartInfo.Arguments += "\" -vframes 1 -f image2 -";

            ffmpeg.StartInfo.CreateNoWindow = true;
            ffmpeg.StartInfo.UseShellExecute = false;
            ffmpeg.StartInfo.RedirectStandardError = true;
            ffmpeg.StartInfo.RedirectStandardOutput = true;

            ffmpeg.Start();

            do
            {
               Thread.Sleep(waitInterval);
               totalWaitTime += waitInterval;
            }
            while ((!ffmpeg.HasExited) &&
                   (totalWaitTime < Configuration.GetSection("FFmpeg:Timeout").Get<Int32>()));

            if (ffmpeg.HasExited)
            {
               Stream baseStream = ffmpeg.StandardOutput.BaseStream;

               using (var memoryStream = new MemoryStream())
               {
                  baseStream.CopyTo(memoryStream);
                  output = memoryStream.ToArray();
               }

               /*
               if (output.Length > 0)
               {
                  Debug.Write(".");
               }
               else
               {
                  var error = ffmpeg.StandardError.ReadToEnd();

                  if (error.Length > 0)
                  {
                     Debug.WriteLine("o");
                     Debug.WriteLine("Arguments: " + ffmpeg.StartInfo.Arguments);
                     Debug.WriteLine(error);
                  }
                  else
                  {
                     Debug.Write("o");
                  }
               }
               */
            }
            else
            {
               // It's been too long. Kill it!
               ffmpeg.Kill();

               // Debug.Write("x");
            }
         }

         return output;
      }

      /// <summary>
      /// Overrides the GenerateThumbnails() method of the MediaFile generating thumbnails for the
      /// video file.
      /// </summary>
      /// <returns>The count of successfully generated thumbnails.</returns>
      public override int GenerateThumbnails(bool force = false)
      {
         int total = 0;
         var nullColums = Thumbnails.NullColumns;

         // Make sure the video file is valid and not corrupted or empty.
         if ((Size == 0) || (Resolution.Height == 0) || (Resolution.Width == 0))
         {
            return total;
         }

         // TODO: Improve the thumbnail generation by generating a .webm file:
         //       ffmpeg -i happy-birthday.mp4 -vf select="eq(pict_type\,I),scale=720:-1,fps=1/5" -f image2pipe - | ffmpeg -f image2pipe -r 1 -c:v mjpeg -i - -c:v libvpx-vp9 -b:v 0 -crf 20 test.webm

         /*----------------------------------------------------------------------------------
                                         GENERATE THUMBNAILS
         ----------------------------------------------------------------------------------*/

         // Debug.Write("GENERATING THUMBNAILS: " + FullPath);

         // Debug.Write(" [");

         foreach (var item in ThumbnailsConfiguration.Value)
         {
            int count = 0;
            int width = -1;
            int height = -1;
            bool crop = false;
            bool sprite = false;
            string label = item.Key;

            if (item.Value.ContainsKey("Width")) width = item.Value["Width"];
            if (item.Value.ContainsKey("Height")) height = item.Value["Height"];
            if (item.Value.ContainsKey("Crop")) crop = (item.Value["Crop"] > 0);
            if (item.Value.ContainsKey("Sprite")) sprite = (item.Value["Sprite"] > 0);
            if (item.Value.ContainsKey("Count")) count = (int)Math.Min(item.Value["Count"], Math.Floor(Duration));

            using var collection = new MagickImageCollection();

            for (int counter = 0; counter < Math.Max(1, count); counter++)
            {
               int position = (int)((counter + 0.5) * Duration / count);
               string column = ((count >= 1) && !sprite) ? String.Format("{0}{1}", item.Key, counter) : label;

               if (!force)
               {
                  // Skip the thumbnail generation for this specific thumbnail if it already exists.
                  if (!nullColums.Contains(column, StringComparer.InvariantCultureIgnoreCase)) continue;
               }

               if (!sprite || (counter == 0)) Logger.LogDebug("Generating The {} Thumbnail For: {}", column, FullPath);

               // Generate the thumbnail.
               byte[] thumbnail = GenerateThumbnail(FullPath, position, width, height, crop);

               if ((thumbnail != null) && (thumbnail.Length > 0))
               {
                  // Add the newly generated thumbnail to the database.
                  if (count >= 1)
                  {
                     if (sprite)
                     {
                        collection.Add(new MagickImage(thumbnail));
                     }
                     else
                     {
                        Thumbnails[counter] = thumbnail;
                     }
                  }
                  else
                  {
                     Thumbnails[label] = thumbnail;
                  }

                  total++;
               }
               else
               {
                  if (collection.Count > 0)
                  {
                     var last = collection.Last();
                     collection.Add(new MagickImage(new MagickColor(0, 0, 0, 0), last.Width, last.Height));
                  }
                  else
                  {
                     break;
                  }
               }
            }

            if (collection.Count > 0)
            {
               using var output = collection.AppendHorizontally();
               Thumbnails[label] = output.ToByteArray();
            }
         }

         // Debug.WriteLine("]");

         if (total > 0)
         {
            Modified = true;
         }

         return total;
      }

      public string GenerateVideoOnDemandPlaylist(int duration)
      {
         double segment = (double)duration;
         var content = new StringBuilder();

         content.AppendLine("#EXTM3U");
         content.AppendLine("#EXT-X-VERSION:6");
         content.AppendLine(String.Format("#EXT-X-TARGETDURATION:{0}", segment));
         content.AppendLine("#EXT-X-MEDIA-SEQUENCE:0");
         content.AppendLine("#EXT-X-PLAYLIST-TYPE:VOD");
         content.AppendLine("#EXT-X-INDEPENDENT-SEGMENTS");

         for (double index = 0; (index * segment) < Duration; index++)
         {
            content.AppendLine(String.Format("#EXTINF:{0:#.000000},", ((Duration - (index * segment)) > segment) ? segment : ((Duration - (index * segment)))));
            content.AppendLine(String.Format("{0:00000}.ts", index));
         }

         content.AppendLine("#EXT-X-ENDLIST");

         return content.ToString();
      }

      public byte[] GenerateVideoOnDemandSegment(int index, int duration)
      {
         byte[] output = Array.Empty<byte>();
         int timeout = Configuration.GetSection("FFmpeg:Timeout").Get<Int32>();
         string executable = Configuration["FFmpeg:Path"] + Platform.Separator.Path + "ffmpeg" + Platform.Extension.Executable;
         DirectoryInfo temp = Directory.CreateDirectory(System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName()));
         string format = System.IO.Path.Combine(temp.FullName, "output-%05d.ts");

         int waitInterval = 100;
         int totalWaitTime = 0;

         if (!File.Exists(executable))
         {
            throw new DirectoryNotFoundException("ffmpeg not found at the specified path: " + executable);
         }

         using (Process ffmpeg = new())
         {
            ffmpeg.StartInfo.FileName = executable;

            ffmpeg.StartInfo.Arguments = String.Format("-ss {0} ", index * duration);
            ffmpeg.StartInfo.Arguments += String.Format("-y -t {0} ", duration);
            ffmpeg.StartInfo.Arguments += String.Format("-i \"{0}\" ", FullPath);
            ffmpeg.StartInfo.Arguments += String.Format("-c:v libx264 -c:a aac ");
            ffmpeg.StartInfo.Arguments += String.Format("-segment_time {0} -reset_timestamps 1 -break_non_keyframes 1 -map 0 ", duration);
            ffmpeg.StartInfo.Arguments += String.Format("-initial_offset {0} ", index * duration);
            ffmpeg.StartInfo.Arguments += String.Format("-f segment -segment_format mpegts {0}", format);

            Logger.LogDebug(String.Format("{0} {1}", ffmpeg.StartInfo.FileName, ffmpeg.StartInfo.Arguments));

            ffmpeg.StartInfo.CreateNoWindow = true;
            ffmpeg.StartInfo.UseShellExecute = false;
            ffmpeg.StartInfo.RedirectStandardError = true;
            ffmpeg.StartInfo.RedirectStandardOutput = true;

            ffmpeg.Start();

            do
            {
               Thread.Sleep(waitInterval);
               totalWaitTime += waitInterval;
            }
            while ((!ffmpeg.HasExited) && (totalWaitTime < timeout));

            if (ffmpeg.HasExited)
            {
               string filename = System.IO.Path.Combine(temp.FullName, "output-00000.ts");

               if (!File.Exists(filename))
               {
                  throw new FileNotFoundException("Unable to find the generated segment: " + filename);
               }

               output = File.ReadAllBytes(filename);
            }
            else
            {
               // It's been too long. Kill it!
               ffmpeg.Kill();
            }
         }

         // Remove the temporary directory and all its contents.
         temp.Delete(true);

         return output;
      }

      #endregion // Video File Operations
   }
}
