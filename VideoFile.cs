using System;
using System.IO;
using System.Xml.Linq;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Generic;
using System.Threading;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MediaCurator
{
   class VideoFile : MediaFile
   {
      #region Constants

      private Lazy<Dictionary<string, Dictionary<string, int>>> Configuration => new(() =>
      {
         var section = _configuration.GetSection("Thumbnails:Video");

         if (section.Exists())
         {
            return section.Get<Dictionary<string, Dictionary<string, int>>>();
         }

         return new Dictionary<string, Dictionary<string, int>>();
      });

      #endregion // Constants

      #region Fields

      public double Duration
      {
         get
         {
            string duration = Tools.GetAttributeValue(Self, "Duration");
            return Double.Parse((duration.Length > 0) ? duration : "0.0",
                                 CultureInfo.InvariantCulture);
         }

         set
         {
            Tools.SetAttributeValue(Self, "Duration", value.ToString(CultureInfo.InvariantCulture));
         }
      }

      /// <summary>
      /// Gets the video duration in text to be shown on thumbnails.
      /// </summary>
      /// <value>
      /// The video duration in text format.
      /// </value>
      public string DurationText
      {
         get
         {
            string duration = "";

            if (Duration <= 0)
            {
               return duration;
            }

            TimeSpan timespan = TimeSpan.FromSeconds(Duration);

            if (timespan.Hours > 0.0)
            {
               duration += string.Format("{0:D2}:", timespan.Hours);
            }

            duration += string.Format("{0:D2}:{1:D2}",
                                       timespan.Minutes,
                                       timespan.Seconds);

            return " " + duration + " ";
         }
      }

      public ResolutionType Resolution
      {
         get
         {
            return new ResolutionType(Tools.GetAttributeValue(Self, "Resolution"));
         }

         set
         {
            Tools.SetAttributeValue(Self, "Resolution", value.ToString());
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
            base.Model = value;
         }
      }

      /// <summary>
      /// Gets the tooltip text of this video file.
      /// </summary>
      public override string ToolTip
      {
         get
         {
            string tooltip = "";
            TimeSpan duration = TimeSpan.FromSeconds(Duration);

            tooltip += String.Format("{0}", Name);

            if (Duration < 3600)               // < 1h
            {
               tooltip += String.Format("\nDuration: {0:D2}:{1:D2}",
                                         duration.Minutes, duration.Seconds);
            }
            else                                // >= 1h
            {
               tooltip += String.Format("\nDuration: {0:D2}:{1:D2}:{2:D2}",
                                         duration.Hours, duration.Minutes, duration.Seconds);
            }

            tooltip += String.Format("\nResolution: {0}", Resolution);

            tooltip += String.Format("\nDate Created: {0}", DateCreated);
            tooltip += String.Format("\nDate Modified: {0}", DateModified);

            if (Size < 0x400L)                 // < 1KB
            {
               tooltip += String.Format("\nSize: {0}B", Size);
            }
            else if (Size < 0x100000L)         // < 1MB
            {
               tooltip += String.Format("\nSize: {0:F2}KB", (double)Size / (double)0x400UL);
            }
            else if (Size < 0x40000000L)       // < 1GB
            {
               tooltip += String.Format("\nSize: {0:F2}MB", (double)Size / (double)0x100000UL);
            }
            else if (Size < 0x10000000000L)    // < 1TB
            {
               tooltip += String.Format("\nSize: {0:F2}GB", ((double)Size / (double)0x40000000UL));
            }

            return tooltip;
         }
      }

      #endregion // Fields

      #region Constructors

      public VideoFile(IConfiguration configuration, IThumbnailsDatabase thumbnailsDatabase, IMediaLibrary mediaLibrary, string path)
         : base(configuration, thumbnailsDatabase, mediaLibrary, "Video", path)
      {
         // The base class constructor will take care of the parents and the creation or retrieval
         // of the element itself. Here we'll attend to additional properties of a video file.

         if (Self != null)
         {
            if (Created)
            {
               // This element did not exist before and has been createdn or it did exist but it has
               // been marked as "Modified". Therefore, the missing/updated fields need be filled in here.
            }
         }
      }

      public VideoFile(IConfiguration configuration, IThumbnailsDatabase thumbnailsDatabase, IMediaLibrary mediaLibrary, XElement element, bool update = false)
         : base(configuration, thumbnailsDatabase, mediaLibrary, element, update)
      {
         if (Self != null)
         {
            if (update)
            {
               if (Modified)
               {
                  // This file existed before but has changed since the last scan.
               }
            }
         }
      }

      #endregion // Constructors

      #region Video File Operations

      public override void GetFileInfo(string path)
      {
         string output = null;
         string executable = _configuration["FFmpeg:Path"] + Platform.Separator.Path + "ffprobe" + Platform.Extension.Executable;

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

            ffprobe.WaitForExit(_configuration.GetSection("FFmpeg:Timeout").Get<Int32>());
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
            Debug.WriteLine(FullPath + " : ");
            Debug.WriteLine(e.Message);
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
            Debug.WriteLine(FullPath + " : ");
            Debug.WriteLine(e.Message);
         }
      }

      private byte[] GenerateThumbnail(string path, int position, int width, int height, bool crop)
      {
         byte[] output = null;
         string executable = _configuration["FFmpeg:Path"] + Platform.Separator.Path + "ffmpeg" + Platform.Extension.Executable;

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

            ffmpeg.StartInfo.Arguments += "\" -vframes 1 -f singlejpeg -";

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
                   (totalWaitTime < _configuration.GetSection("FFmpeg:Timeout").Get<Int32>()));

            if (ffmpeg.HasExited)
            {
               FileStream baseStream = ffmpeg.StandardOutput.BaseStream as FileStream;

               using (var memoryStream = new MemoryStream())
               {
                  baseStream.CopyTo(memoryStream);
                  output = memoryStream.ToArray();
               }

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
            }
            else
            {
               // It's been too long. Kill it!
               ffmpeg.Kill();

               Debug.Write("x");
            }
         }

         return output;
      }

      /// <summary>
      /// Overrides the GenerateThumbnails() method of the MediaFile generating thumbnails for the
      /// video file.
      /// </summary>
      /// <returns>The count of successfully generated thumbnails.</returns>
      public override int GenerateThumbnails()
      {
         int total = 0;

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

         Debug.Write("GENERATING THUMBNAILS: " + FullPath);

         Debug.Write(" [");

         foreach (var item in Configuration.Value)
         {
            int count = 0;
            int width = -1;
            int height = -1;
            bool crop = false;
            string label = item.Key;

            if (item.Value.ContainsKey("Count")) count = item.Value["Count"];
            if (item.Value.ContainsKey("Width")) width = item.Value["Width"];
            if (item.Value.ContainsKey("Height")) height = item.Value["Height"];
            if (item.Value.ContainsKey("Crop")) crop = (item.Value["Crop"] > 0);

            for (int counter = 0; counter < Math.Max(1, count); counter++)
            {
               int position = (int)((counter + 0.5) * Duration / Math.Max(24, count));

               // Generate the thumbnail.
               byte[] thumbnail = GenerateThumbnail(FullPath, position, width, height, crop);

               if ((thumbnail != null) && (thumbnail.Length > 0))
               {
                  // Add the newly generated thumbnail to the database.
                  if (count >= 1)
                  {
                     Thumbnails[counter] = thumbnail;
                  }
                  else
                  {
                     Thumbnails[label] = thumbnail;
                  }

                  total++;
               }
            }
         }

         Debug.WriteLine("]");

         return total;
      }

      #endregion // Video File Operations
   }
}