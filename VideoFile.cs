using System.Text;
using System.Text.Json;
using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.Options;
using ImageMagick;
using MediaCurator.Configuration;

namespace MediaCurator
{
   class VideoFile : MediaFile
   {
      #region Constants

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
                       IOptionsMonitor<Settings> settings,
                       IThumbnailsDatabase thumbnailsDatabase,
                       IMediaLibrary mediaLibrary,
                       string? id = null, string? path = null,
                       IProgress<float>? progress = null
      ) : base(logger, services, settings, thumbnailsDatabase, mediaLibrary, id, path, progress)
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
         string executable = System.IO.Path.Combine(Settings.CurrentValue.FFmpeg.Path, "ffprobe" + Platform.Extension.Executable);

         if (!File.Exists(executable))
         {
            throw new DirectoryNotFoundException("ffprobe not found at the specified path: " + executable);
         }

         string[] arguments =
         [
            "-v quiet",                      // Suppress output messages
            "-print_format json",            // Set output format to JSON
            "-show_format",                  // Show file format information
            "-show_streams",                 // Show stream information
            "-select_streams v:0",           // Select the first video stream
            $"\"{path}\""                    // Input file path
         ];

         using (Process process = new())
         {
            process.StartInfo = new ProcessStartInfo
            {
               FileName = executable,
               Arguments = string.Join(" ", arguments),
               CreateNoWindow = true,
               UseShellExecute = false,
               RedirectStandardOutput = true,
               RedirectStandardError = true
            };

            Logger.LogTrace("{FileName} {Arguments}", process.StartInfo.FileName, process.StartInfo.Arguments);

            process.Start();

            Task<string> errorTask = process.StandardError.ReadToEndAsync();
            output = process.StandardOutput.ReadToEnd();

            process.WaitForExit(Settings.CurrentValue.FFmpeg.TimeoutMilliseconds);

            if (process.HasExited)
            {
               if (process.ExitCode != 0)
               {
                  Logger.LogDebug("{Errors}", errorTask.Result);
               }
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
            Resolution = new ResolutionType(string.Format("{0}x{1}",
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

         string executable = System.IO.Path.Combine(Settings.CurrentValue.FFmpeg.Path, $"ffmpeg{Platform.Extension.Executable}");

         int waitInterval = 100;
         int totalWaitTime = 0;

         if (!File.Exists(executable))
         {
            throw new DirectoryNotFoundException("ffmpeg not found at the specified path: " + executable);
         }

         string[] arguments =
         [
            // Seek to the specified position
            $"-ss {position}",
            // Input file
            $"-i \"{path}\"",
            // Overwrite output files
            "-y",
            // Select I-frames and set scale
            $"-vf select=\"eq(pict_type\\,I),scale={width}:{height}" +
            // Add cropping if requested
            (crop ? (height > 0 ? $",crop=iw:'min({height},ih)'\"" : $",crop=iw:'min(iw/16*9,ih)'\"") : "\""),
            // Output only one frame
            "-vframes 1",
            // Output format: image
            "-f image2",
            // Output to stdout
            "-",
         ];

         using (Process process = new())
         {
            process.StartInfo = new ProcessStartInfo
            {
               FileName = executable,
               Arguments = string.Join(" ", arguments),
               CreateNoWindow = true,
               UseShellExecute = false,
               RedirectStandardError = true,
               RedirectStandardOutput = true
            };

            Logger.LogTrace("{FileName} {Arguments}", process.StartInfo.FileName, process.StartInfo.Arguments);

            process.Start();

            Task<string> errorTask = process.StandardError.ReadToEndAsync();

            do
            {
               Thread.Sleep(waitInterval);
               totalWaitTime += waitInterval;
            }
            while (!process.HasExited && totalWaitTime < Settings.CurrentValue.FFmpeg.TimeoutMilliseconds);

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
         foreach (var item in Settings.CurrentValue.Thumbnails.Video)
         {
            total += (item.Value.Count > 0) ? (int)Math.Min(item.Value.Count, Math.Floor(Duration)) : 1;
         }

         Progress?.Report(0.0f);

         foreach (var item in Settings.CurrentValue.Thumbnails.Video)
         {
            // No point in generating thumbnails for a zero-length file
            if (Duration == 0.0f) break;

            string label = item.Key;
            bool crop = item.Value.Crop;
            bool sprite = item.Value.Sprite;
            int height = item.Value.Height;
            int width = item.Value.Width;
            int count = Math.Min(item.Value.Count, (int)Math.Floor(Duration));

            using var collection = new MagickImageCollection();

            for (int counter = 0; counter < Math.Max(1, count); counter++)
            {
               byte[]? thumbnail = null;
               int position = (int)((counter + 0.5) * Duration / (count != 0 ? count : Math.Min(24, Math.Floor(Duration))));
               string column = ((count >= 1) && !sprite) ? string.Format("{0}{1}", item.Key, counter) : label;

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
            content.AppendLine(string.Format("#EXT-X-STREAM-INF:BANDWIDTH=45000000,RESOLUTION={0}x2160,CODECS=\"avc1.640033,mp4a.40.2\"", (long)((Resolution.Width * 2160) / Resolution.Height)));
            content.AppendLine("2160.m3u8\n");
         }

         if (Resolution.Height >= 1080)
         {
            content.AppendLine(string.Format("#EXT-X-STREAM-INF:BANDWIDTH=6000000,RESOLUTION={0}x1080,CODECS=\"avc1.640028,mp4a.40.2\"", (long)((Resolution.Width * 1080) / Resolution.Height)));
            content.AppendLine("1080.m3u8\n");
         }

         if (Resolution.Height >= 720)
         {
            content.AppendLine(string.Format("#EXT-X-STREAM-INF:BANDWIDTH=4000000,RESOLUTION={0}x720,CODECS=\"avc1.64001f,mp4a.40.2\"", (long)((Resolution.Width * 720) / Resolution.Height)));
            content.AppendLine("720.m3u8\n");
         }

         if (Resolution.Height >= 480)
         {
            content.AppendLine(string.Format("#EXT-X-STREAM-INF:BANDWIDTH=2000000,RESOLUTION={0}x480,CODECS=\"avc1.64001e,mp4a.40.2\"", (long)((Resolution.Width * 480) / Resolution.Height)));
            content.AppendLine("480.m3u8\n");
         }

         if (Resolution.Height >= 360)
         {
            content.AppendLine(string.Format("#EXT-X-STREAM-INF:BANDWIDTH=1000000,RESOLUTION={0}x360,CODECS=\"avc1.640015,mp4a.40.2\"", (long)((Resolution.Width * 360) / Resolution.Height)));
            content.AppendLine("360.m3u8\n");
         }

         if (Resolution.Height >= 240)
         {
            content.AppendLine(string.Format("#EXT-X-STREAM-INF:BANDWIDTH=700000,RESOLUTION={0}x240,CODECS=\"avc1.64000d,mp4a.40.2\"", (long)((Resolution.Width * 240) / Resolution.Height)));
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
         content.AppendLine(string.Format("#EXT-X-TARGETDURATION:{0}", segment));
         content.AppendLine("#EXT-X-MEDIA-SEQUENCE:0");
         content.AppendLine("#EXT-X-PLAYLIST-TYPE:VOD");
         content.AppendLine("#EXT-X-INDEPENDENT-SEGMENTS");

         for (double index = 0; (index * interval) < Duration; index++)
         {
            content.AppendLine(string.Format("#EXTINF:{0:#.000000},", ((Duration - (index * interval)) > interval) ? interval : ((Duration - (index * interval)))));
            if (!string.IsNullOrEmpty(quality)) content.AppendLine(string.Format("{0}/{1:00000}.ts", quality, index));
            else content.AppendLine(string.Format("{0:00000}.ts", index));
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
         byte[] output = [];
         string executable = System.IO.Path.Combine(Settings.CurrentValue.FFmpeg.Path, $"ffmpeg{Platform.Extension.Executable}");
         DirectoryInfo temp = Directory.CreateDirectory(System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName()));
         string format = System.IO.Path.Combine(temp.FullName, "output-%05d.ts");

         int waitInterval = 100;
         int totalWaitTime = 0;

         if (!File.Exists(executable))
         {
            throw new DirectoryNotFoundException("ffmpeg not found at the specified path: " + executable);
         }

         string[] arguments =
         [
            // Start time
            $"-ss {sequence * duration}",
            // Duration of 10 seconds
            (count == 0) ? $"-t {Duration - sequence * duration}" : $"-t {count * duration}",
            // Copy timestamps
            $"-copyts",
            // Input file
            $"-i \"{FullPath}\" ",
            // Map all streams from the input
            $"-map 0",
            // Exclude all subtitle streams
            $"-map -0:s",
            // Set video codec to libx264
            $"-c:v h264_videotoolbox",
            // Set audio codec to AAC
            $"-c:a aac",
            // Segment duration in seconds
            $"-segment_time {duration}",
            // Reset timestamps
            $"-reset_timestamps 0",
            // Break segments at non-keyframes
            $"-break_non_keyframes 1",
            // $"-initial_offset {sequence * duration}",
            // Output format: segment
            $"-f segment",
            // Segment format: MPEG-TS
            $"-segment_format mpegts",
            // Output file pattern
            $"{format}",
            // Overwrite output files without asking
            "-y",
         ];

         string template = "-vf scale=-1:{0} -b:v {1}k -maxrate {2}k -bufsize {3}k -b:a 128k";

         var qualitySettings = new Dictionary<string, (int height, int bitrate, int maxrate, int bufsize)>
         {
            { "240p",   (240, 700, 700, 1400) },
            { "240",    (240, 700, 700, 1400) },
            { "360p",   (360, 1000, 1000, 2000) },
            { "360",    (360, 1000, 1000, 2000) },
            { "480p",   (480, 2000, 2000, 4000) },
            { "480",    (480, 2000, 2000, 4000) },
            { "720p",   (720, 4000, 4000, 8000) },
            { "720",    (720, 4000, 4000, 8000) },
            { "1080p",  (1080, 6000, 6000, 12000) },
            { "1080",   (1080, 6000, 6000, 12000) },
            { "4k",     (2160, 45000, 45000, 90000) },
            { "2160p",  (2160, 45000, 45000, 90000) },
            { "2160",   (2160, 45000, 45000, 90000) }
         };

         if (qualitySettings.TryGetValue(quality, out var settings))
         {
            _ = arguments.Append(string.Format(template, settings.height, settings.bitrate, settings.maxrate, settings.bufsize));
         }

         using (Process process = new())
         {
            process.StartInfo = new ProcessStartInfo
            {
               FileName = executable,
               Arguments = string.Join(" ", arguments),
               RedirectStandardOutput = true,
               RedirectStandardError = true,
               RedirectStandardInput = false,
               UseShellExecute = false,
               CreateNoWindow = true
            };

            Logger.LogTrace("{FileName} {Arguments}", process.StartInfo.FileName, process.StartInfo.Arguments);

            process.Start();

            do
            {
               Thread.Sleep(waitInterval);
               totalWaitTime += waitInterval;
            }
            while ((!process.HasExited) && (totalWaitTime < Settings.CurrentValue.FFmpeg.TimeoutMilliseconds));

            if (!process.HasExited || (process.ExitCode != 0))
            {
               if (!process.HasExited) process.Kill();

               throw new Exception(string.Format(
                  "Segment generation failed: {0} {1}\n{2}",
                  process.StartInfo.FileName, process.StartInfo.Arguments, process.StandardError.ReadToEnd()));
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
