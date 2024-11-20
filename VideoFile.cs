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

         return section.Exists() ? section.Get<Dictionary<string, Dictionary<string, int>>>() ?? [] : [];

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
                       string? id = null, string? path = null,
                       IProgress<float>? progress = null
      ) : base(logger, services, configuration, thumbnailsDatabase, mediaLibrary, id, path, progress)
      {
         // The base class constructor will take care of the entry, its general attributes and its
         // parents and below we'll take care of its specific attributes.

         if (Skipped) return;
      }

      #endregion // Constructors

      #region Video File Operations

      public override void GetFileInfo(string path)
      {
         string? output = null;
         string executable = Configuration["FFmpeg:Path"] + Platform.Separator.Path + "ffprobe" + Platform.Extension.Executable;

         if (!File.Exists(executable))
         {
            throw new DirectoryNotFoundException("ffprobe not found at the specified path: " + executable);
         }

         using (Process process = new())
         {
            process.StartInfo.FileName = executable;
            process.StartInfo.Arguments = "-v quiet -print_format json -show_format ";
            process.StartInfo.Arguments += "-show_streams -select_streams v:0 " + "\"" + path + "\"";
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;

            Logger.LogTrace("{FileName} {Arguments}", process.StartInfo.FileName, process.StartInfo.Arguments);

            process.Start();

            Task<string> errorTask = process.StandardError.ReadToEndAsync();

            output = process.StandardOutput.ReadToEnd();

            process.WaitForExit(Configuration.GetSection("FFmpeg:Timeout").Get<Int32>());

            if (process.HasExited)
            {
               if (process.ExitCode != 0) Logger.LogDebug("{Errors}", errorTask.Result);
            }
            else
            {
               Logger.LogDebug("File Info Retrieval Timeout: {FullPath}", FullPath);
            }
         }

         var fileInfo = JsonDocument.Parse(output).RootElement;

         /*--------------------------------------------------------------------------------
                                           DURATION
         --------------------------------------------------------------------------------*/

         try
         {
            var duration = fileInfo.GetProperty("format").GetProperty("duration").GetString();

            if (!string.IsNullOrEmpty(duration))
            {
               Duration = double.Parse(duration, CultureInfo.InvariantCulture);
            }
            else
            {
               Logger.LogDebug("Duration Property Missing Or Null For: {}", FullPath);
            }
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

      private byte[]? GenerateThumbnail(string path, int position, int width, int height, bool crop)
      {
         byte[]? output = null;

         string executable = Configuration["FFmpeg:Path"] + Platform.Separator.Path + "ffmpeg" + Platform.Extension.Executable;

         int waitInterval = 100;
         int totalWaitTime = 0;

         if (!File.Exists(executable))
         {
            throw new DirectoryNotFoundException("ffmpeg not found at the specified path: " + executable);
         }

         using (Process process = new())
         {
            process.StartInfo.FileName = executable;

            process.StartInfo.Arguments = String.Format("-ss {0} -i \"{1}\" ", position.ToString(), path);
            process.StartInfo.Arguments += String.Format("-y -vf select=\"eq(pict_type\\,I),scale={0}:{1}", width.ToString(), height.ToString());

            if (crop)
            {
               process.StartInfo.Arguments += String.Format(",crop=iw:'min({0},ih)'", (height > 0) ? height.ToString() : "iw/16*9");
            }

            process.StartInfo.Arguments += "\" -vframes 1 -f image2 -";

            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardOutput = true;

            Logger.LogTrace("{FileName} {Arguments}", process.StartInfo.FileName, process.StartInfo.Arguments);

            process.Start();

            Task<string> errorTask = process.StandardError.ReadToEndAsync();

            do
            {
               Thread.Sleep(waitInterval);
               totalWaitTime += waitInterval;
            }
            while ((!process.HasExited) &&
                   (totalWaitTime < Configuration.GetSection("FFmpeg:Timeout").Get<Int32>()));

            if (process.HasExited)
            {
               if (process.ExitCode != 0)
               {
                  Logger.LogDebug("{Errors}", errorTask.Result);

                  return null;
               }

               Stream baseStream = process.StandardOutput.BaseStream;
               using var memoryStream = new MemoryStream();
               baseStream.CopyTo(memoryStream);
               output = memoryStream.ToArray();
            }
            else
            {
               // It's been too long. Kill it!
               process.Kill();

               Logger.LogDebug("Thumbnail Generation Timeout: {FullPath}", FullPath);
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
         int thumbnails = 0, total = 0, generated = 0;
         string[] nullColumns = Thumbnails?.NullColumns ?? [];

         // Make sure the video file is valid and not corrupted or empty.
         if ((Size == 0) || (Resolution.Height == 0) || (Resolution.Width == 0))
         {
            return thumbnails;
         }

         // TODO: Improve the thumbnail generation by generating a .webm file:
         //       ffmpeg -i happy-birthday.mp4 -vf select="eq(pict_type\,I),scale=720:-1,fps=1/5" -f image2pipe - | ffmpeg -f image2pipe -r 1 -c:v mjpeg -i - -c:v libvpx-vp9 -b:v 0 -crf 20 test.webm

         /*----------------------------------------------------------------------------------
                                         GENERATE THUMBNAILS
         ----------------------------------------------------------------------------------*/

         // Calculate the total number of thumbnails to be generated used for progress reporting
         foreach (var item in ThumbnailsConfiguration.Value)
         {
            if (item.Value.TryGetValue("Count", out var value))
            {
               total += (int)Math.Min(value, Math.Floor(Duration));
            }
            else
            {
               total += 1;
            }
         }

         Progress?.Report(0.0f);

         foreach (var item in ThumbnailsConfiguration.Value)
         {
            // No point in generating thumbnails for a zero-length file
            if (Duration == 0.0f) break;

            int count = 0;
            int width = -1;
            int height = -1;
            bool crop = false;
            bool sprite = false;
            string label = item.Key;

            if (item.Value.ContainsKey("Width")) width = item.Value["Width"];
            if (item.Value.ContainsKey("Height")) height = item.Value["Height"];
            if (item.Value.ContainsKey("Crop")) crop = item.Value["Crop"] > 0;
            if (item.Value.ContainsKey("Sprite")) sprite = item.Value["Sprite"] > 0;
            if (item.Value.ContainsKey("Count")) count = (int)Math.Min(item.Value["Count"], Math.Floor(Duration));

            using var collection = new MagickImageCollection();

            for (int counter = 0; counter < Math.Max(1, count); counter++)
            {
               byte[]? thumbnail = null;
               int position = (int)((counter + 0.5) * Duration / (count != 0 ? count : Math.Min(24, Math.Floor(Duration))));
               string column = ((count >= 1) && !sprite) ? String.Format("{0}{1}", item.Key, counter) : label;

               if (!force)
               {
                  // Skip the thumbnail generation for this specific thumbnail if it already exists
                  if (!nullColumns.Contains(column, StringComparer.InvariantCultureIgnoreCase)) continue;
               }

               if (!sprite || (counter == 0)) Logger.LogDebug("Generating The {} Thumbnail For: {}", column, FullPath);

               // Generate the thumbnail
               if (!string.IsNullOrEmpty(FullPath)) thumbnail = GenerateThumbnail(FullPath, position, width, height, crop);

               // Report the progress
               Progress?.Report((float)++generated / (float)total);

               if ((thumbnail != null) && (thumbnail.Length > 0))
               {
                  // Add the newly generated thumbnail to the database
                  if (count >= 1)
                  {
                     if (sprite)
                     {
                        collection.Add(new MagickImage(thumbnail));
                     }
                     else
                     {
                        if (Thumbnails is not null) Thumbnails[counter] = thumbnail;
                     }
                  }
                  else
                  {
                     if (Thumbnails is not null) Thumbnails[label] = thumbnail;
                  }

                  thumbnails++;
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

               if (Thumbnails is not null) Thumbnails[label] = output.ToByteArray();
            }
         }

         Progress?.Report(1.0f);

         if (thumbnails > 0)
         {
            Modified = true;
         }

         return thumbnails;
      }

      public string GeneratePlaylist()
      {
         var content = new StringBuilder();

         content.AppendLine("#EXTM3U");
         content.AppendLine("#EXT-X-VERSION:7");

         if (Resolution.Height >= 2160)
         {
            content.AppendLine(String.Format("#EXT-X-STREAM-INF:BANDWIDTH=45000000,RESOLUTION={0}x2160,CODECS=\"avc1.640033,mp4a.40.2\"", (long)((Resolution.Width * 2160) / Resolution.Height)));
            content.AppendLine("2160.m3u8\n");
         }

         if (Resolution.Height >= 1080)
         {
            content.AppendLine(String.Format("#EXT-X-STREAM-INF:BANDWIDTH=6000000,RESOLUTION={0}x1080,CODECS=\"avc1.640028,mp4a.40.2\"", (long)((Resolution.Width * 1080) / Resolution.Height)));
            content.AppendLine("1080.m3u8\n");
         }

         if (Resolution.Height >= 720)
         {
            content.AppendLine(String.Format("#EXT-X-STREAM-INF:BANDWIDTH=4000000,RESOLUTION={0}x720,CODECS=\"avc1.64001f,mp4a.40.2\"", (long)((Resolution.Width * 720) / Resolution.Height)));
            content.AppendLine("720.m3u8\n");
         }

         if (Resolution.Height >= 480)
         {
            content.AppendLine(String.Format("#EXT-X-STREAM-INF:BANDWIDTH=2000000,RESOLUTION={0}x480,CODECS=\"avc1.64001e,mp4a.40.2\"", (long)((Resolution.Width * 480) / Resolution.Height)));
            content.AppendLine("480.m3u8\n");
         }

         if (Resolution.Height >= 360)
         {
            content.AppendLine(String.Format("#EXT-X-STREAM-INF:BANDWIDTH=1000000,RESOLUTION={0}x360,CODECS=\"avc1.640015,mp4a.40.2\"", (long)((Resolution.Width * 360) / Resolution.Height)));
            content.AppendLine("360.m3u8\n");
         }

         if (Resolution.Height >= 240)
         {
            content.AppendLine(String.Format("#EXT-X-STREAM-INF:BANDWIDTH=700000,RESOLUTION={0}x240,CODECS=\"avc1.64000d,mp4a.40.2\"", (long)((Resolution.Width * 240) / Resolution.Height)));
            content.AppendLine("240.m3u8\n");
         }

         return content.ToString();
      }

      public string GeneratePlaylist(int segment, string quality = "")
      {
         double interval = (double)segment;
         var content = new StringBuilder();

         content.AppendLine("#EXTM3U");
         content.AppendLine("#EXT-X-VERSION:6");
         content.AppendLine(String.Format("#EXT-X-TARGETDURATION:{0}", segment));
         content.AppendLine("#EXT-X-MEDIA-SEQUENCE:0");
         content.AppendLine("#EXT-X-PLAYLIST-TYPE:VOD");
         content.AppendLine("#EXT-X-INDEPENDENT-SEGMENTS");

         for (double index = 0; (index * interval) < Duration; index++)
         {
            content.AppendLine(String.Format("#EXTINF:{0:#.000000},", ((Duration - (index * interval)) > interval) ? interval : ((Duration - (index * interval)))));
            if (!string.IsNullOrEmpty(quality)) content.AppendLine(String.Format("{0}/{1:00000}.ts", quality, index));
            else content.AppendLine(String.Format("{0:00000}.ts", index));
         }

         content.AppendLine("#EXT-X-ENDLIST");

         return content.ToString();
      }

      /// <summary>
      /// Generates one or more segments of the video starting from the sequence number.
      /// </summary>
      /// <param name="quality">The expected quality e.g. 240p, 360p, ..., 2160p or anything else to disable scaling.</param>
      /// <param name="sequence">The sequence number to start from.</param>
      /// <param name="duration">The duration of each segment in seconds.</param>
      /// <param name="count">The number of segments to generate. If left 0, the segmentation will continue for the rest of the duration.</param>
      /// <returns>The generated segment in mpegts format and byte[] array.</returns>
      /// <exception cref="DirectoryNotFoundException"></exception>
      /// <exception cref="Exception"></exception>
      /// <exception cref="FileNotFoundException"></exception>
      public byte[] GenerateSegments(string quality, int sequence, int duration, int count = 0)
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

            // Start time
            ffmpeg.StartInfo.Arguments = String.Format("-ss {0} ", sequence * duration);
            // Duration of 10 seconds
            ffmpeg.StartInfo.Arguments += String.Format("-t {0} ", count == 0 ? Duration - (double)(sequence * duration) : count * duration);
            // Copy timestamps
            ffmpeg.StartInfo.Arguments += String.Format("-copyts ");
            // Input file
            ffmpeg.StartInfo.Arguments += String.Format("-i \"{0}\" ", FullPath);
            // Map all streams from the input
            ffmpeg.StartInfo.Arguments += String.Format("-map 0 ");
            // Exclude all subtitle streams
            ffmpeg.StartInfo.Arguments += String.Format("-map -0:s ");
            // Set video codec to libx264
            ffmpeg.StartInfo.Arguments += String.Format("-c:v libx264 ");
            // Set audio codec to AAC
            ffmpeg.StartInfo.Arguments += String.Format("-c:a aac ");

            string template = "-vf scale=-1:{0} -b:v {1}k -maxrate {2}k -bufsize {3}k -b:a 128k ";

            switch (quality)
            {
               case "240p":
               case "240":
                  ffmpeg.StartInfo.Arguments += String.Format(template, 240, 700, 700, 1400);
                  break;
               case "360p":
               case "360":
                  ffmpeg.StartInfo.Arguments += String.Format(template, 360, 1000, 1000, 2000);
                  break;
               case "480p":
               case "480":
                  ffmpeg.StartInfo.Arguments += String.Format(template, 480, 2000, 2000, 4000);
                  break;
               case "720p":
               case "720":
                  ffmpeg.StartInfo.Arguments += String.Format(template, 720, 4000, 4000, 8000);
                  break;
               case "1080p":
               case "1080":
                  ffmpeg.StartInfo.Arguments += String.Format(template, 1080, 6000, 6000, 12000);
                  break;
               case "4k":
               case "2160p":
               case "2160":
                  ffmpeg.StartInfo.Arguments += String.Format(template, 2160, 45000, 45000, 90000);
                  break;
            }

            // Segment duration in seconds
            ffmpeg.StartInfo.Arguments += String.Format("-segment_time {0} ", duration);
            // Reset timestamps
            ffmpeg.StartInfo.Arguments += String.Format("-reset_timestamps 0 ");
            // Break segments at non-keyframes
            ffmpeg.StartInfo.Arguments += String.Format("-break_non_keyframes 1 ");

            // ffmpeg.StartInfo.Arguments += String.Format("-initial_offset {0} ", sequence * duration);

            // Output format: segment
            ffmpeg.StartInfo.Arguments += String.Format("-f segment ");
            // Segment format: MPEG-TS
            ffmpeg.StartInfo.Arguments += String.Format("-segment_format mpegts ");
            // Output file pattern
            ffmpeg.StartInfo.Arguments += String.Format("{0} ", format);
            // Overwrite output files without asking
            ffmpeg.StartInfo.Arguments += String.Format("-y", format);

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

            if (!ffmpeg.HasExited || (ffmpeg.ExitCode != 0))
            {
               if (!ffmpeg.HasExited) ffmpeg.Kill();

               throw new Exception(String.Format("Segment generation failed: {0} {1}\n{2}",
                  ffmpeg.StartInfo.FileName, ffmpeg.StartInfo.Arguments, ffmpeg.StandardError.ReadToEnd()));
            }

            string filename = System.IO.Path.Combine(temp.FullName, "output-00000.ts");

            if (!File.Exists(filename))
            {
               throw new FileNotFoundException("Unable to find the generated segment: " + filename);
            }

            output = File.ReadAllBytes(filename);
         }

         temp.Delete(true);

         return output;
      }

      #endregion // Video File Operations
   }
}
