/*
 *  Copyright © 2024 Travelonium AB
 *
 *  This file is part of Arcadeia.
 *
 *  Arcadeia is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU Affero General Public License as published
 *  by the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  Arcadeia is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU Affero General Public License for more details.
 *
 *  You should have received a copy of the GNU Affero General Public License
 *  along with Arcadeia. If not, see <https://www.gnu.org/licenses/>.
 *
 */

using System.Text;
using System.Text.Json;
using System.Diagnostics;
using System.IO.Compression;
using System.Globalization;
using Microsoft.Extensions.Options;
using ImageMagick;
using Arcadeia.Configuration;

namespace Arcadeia
{
   class VideoFile : MediaFile
   {
      #region Constants

      #endregion // Constants

      #region Fields

      /// <summary>
      /// Gets or sets the duration of the video file.
      /// </summary>
      /// <value>
      /// The video file duration.
      /// </value>
      public double? Duration { get; set; }

      private long? _width;
      private long? _height;

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
            _width = value.Width;
            _height = value.Height;
         }
      }

      /// <summary>
      /// Gets or sets the full plain-text transcript of the video file's speech, if any.
      /// </summary>
      public string? Transcript { get; set; }

      /// <summary>
      /// Gets or sets the language detected/used while transcribing the video file.
      /// </summary>
      public string? TranscriptLanguage { get; set; }

      /// <summary>
      /// Gets or sets the JSON-serialized, timestamped transcript segments of the video file.
      /// Unused (null) when the video has embedded subtitle streams - see <see cref="Subtitles"/>.
      /// </summary>
      public string? TranscriptSegments { get; set; }

      /// <summary>
      /// Gets or sets the JSON-serialized list of the video file's embedded subtitle streams
      /// (index/language/codec), if any. When present, these are used instead of a whisper.cpp
      /// transcript for playback captions.
      /// </summary>
      public string? Subtitles { get; set; }

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
            model.Transcript = Transcript;
            model.TranscriptLanguage = TranscriptLanguage;
            model.TranscriptSegments = TranscriptSegments;
            model.Subtitles = Subtitles;

            return model;
         }

         set
         {
            if (value == null) return;

            base.Model = value;

            Duration = value.Duration;
            Resolution = new(value.Width, value.Height);
            Transcript = value.Transcript;
            TranscriptLanguage = value.TranscriptLanguage;
            TranscriptSegments = value.TranscriptSegments;
            Subtitles = value.Subtitles;
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

      public override void GetFileInfo(string path, long size)
      {
         if (size == 0)
         {
            Duration = null;
            Resolution = new ResolutionType();

            return;
         }

         string? output = null;
         string executable = System.IO.Path.Combine(Settings.CurrentValue.FFmpeg.Path ?? "", "ffprobe" + Platform.Extension.Executable);

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
               Arguments = string.Join(" ", arguments.Where(x => !string.IsNullOrEmpty(x)).ToArray()),
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

            if (!process.HasExited || process.ExitCode != 0)
            {
               if (!process.HasExited)
               {
                  Logger.LogWarning("FFprobe Execution Timeout: {FileName} {Arguments}\n{Result}",
                                    process.StartInfo.FileName, process.StartInfo.Arguments, errorTask.Result);
                  process.Kill();
               }
               else
               {
                  Logger.LogWarning("FFprobe Execution Failed: {FileName} {Arguments}\n{Result}",
                                    process.StartInfo.FileName, process.StartInfo.Arguments, errorTask.Result);
               }
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

      public static byte[]? FFmpeg(string executable, string[] arguments, int timeout, bool output = false, ILogger? logger = null)
      {
         using Process process = new()
         {
            StartInfo = new ProcessStartInfo
            {
               FileName = executable,
               Arguments = string.Join(" ", arguments.Where(x => !string.IsNullOrEmpty(x)).ToArray()),
               RedirectStandardOutput = output,
               RedirectStandardError = true,
               UseShellExecute = false,
               CreateNoWindow = true
            }
         };

         logger?.LogTrace("{FileName} {Arguments}", process.StartInfo.FileName, process.StartInfo.Arguments);

         process.Start();
         Task<string> errorTask = process.StandardError.ReadToEndAsync();

         // Standard output must be drained concurrently, not after WaitForExit(): once the child's
         // output exceeds the OS pipe buffer (a few tens of KB), it blocks on write() until someone
         // reads the pipe, which would otherwise deadlock here until the timeout fires. This only
         // showed up once a caller (audio extraction) started producing output larger than a single
         // small thumbnail frame.
         Task<MemoryStream>? outputTask = output ? Task.Run(async () =>
         {
            var memoryStream = new MemoryStream();

            try
            {
               await process.StandardOutput.BaseStream.CopyToAsync(memoryStream);
            }
            catch
            {
               // The process may have been killed due to a timeout, closing the pipe mid-read.
            }

            return memoryStream;
         }) : null;

         if (!process.WaitForExit(timeout))
         {
            logger?.LogWarning("FFmpeg Execution Timeout: {FileName} {Arguments}", process.StartInfo.FileName, process.StartInfo.Arguments);
            process.Kill(true);
            return null;
         }

         string error = errorTask.GetAwaiter().GetResult();

         if (process.ExitCode != 0)
         {
            logger?.LogWarning("FFmpeg Execution Failed: {FileName} {Arguments}\n{Error}", process.StartInfo.FileName, process.StartInfo.Arguments, error);
            return null;
         }

         if (outputTask != null)
         {
            using var memoryStream = outputTask.GetAwaiter().GetResult();
            return memoryStream.ToArray();
         }

         return null;
      }

      private byte[]? GenerateThumbnail(string path, int position, int width, int height, bool crop)
      {
         string executable = System.IO.Path.Combine(Settings.CurrentValue.FFmpeg.Path ?? "", $"ffmpeg{Platform.Extension.Executable}");

         string[] arguments =
         [
            // Seek to the specified position
            $"-ss {position}",
            // Input file
            $"-i \"{path}\"",
            // Overwrite output files
            "-y",
            // Select I-frames and set scale
            $"-vf select=\"eq(pict_type\\,I)+not(mod(n\\,10)),scale={width}:{height}" +
            // Add cropping if requested
            (crop ? (height > 0 ? $",crop=iw:'min({height},ih)'\"" : $",crop=iw:'min(iw/16*9,ih)'\"") : "\""),
            // Output only one frame
            "-frames:v 1",
            // Output format: image
            "-f image2",
            // Output to stdout
            "-",
         ];

         return FFmpeg(executable, arguments, Settings.CurrentValue.FFmpeg.TimeoutMilliseconds, true, Logger);
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
            total += (item.Value.Count > 0) ? (int)Math.Min(item.Value.Count, Math.Floor(Duration ?? -1)) : 1;
         }

         Progress?.Report(0.0f);

         foreach (var item in Settings.CurrentValue.Thumbnails.Video)
         {
            double duration = Duration ?? 0.0f;

            // No point in generating thumbnails for a zero-length file
            if (duration == 0.0f) break;

            string label = item.Key;
            bool crop = item.Value.Crop;
            bool sprite = item.Value.Sprite;
            int height = item.Value.Height;
            int width = item.Value.Width;
            int count = Math.Min(item.Value.Count, (int)Math.Floor(duration));

            using var collection = new MagickImageCollection();

            for (int counter = 0; counter < Math.Max(1, count); counter++)
            {
               byte[]? thumbnail = null;
               int position = (int)((counter + 0.5) * duration / (count != 0 ? count : Math.Min(24, Math.Floor(duration))));
               string column = ((count >= 1) && !sprite) ? string.Format("{0}{1}", item.Key, counter) : label;

               if (!force)
               {
                  // Skip the thumbnail generation for this specific thumbnail if it already exists
                  if (!nullColumns.Contains(column, StringComparer.InvariantCultureIgnoreCase)) continue;
               }

               if (!sprite || (counter == 0)) Logger.LogTrace("Generating The {} Thumbnail For: {}", column, FullPath);

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

         return thumbnails;
      }

      private readonly record struct TranscriptSegment(long Start, long End, string Text);

      private readonly record struct EmbeddedSubtitle(int Index, string? Language, string Codec);

      // Only the default model is bundled in the Docker image; any other model configured via
      // Transcription.Model (e.g. picking a larger checkpoint for better accuracy) is fetched here
      // the first time it's needed rather than bloating the image with every possible model. Guarded
      // by a static lock since multiple ParallelTasks transcription workers could otherwise race to
      // download the same missing model file at once.
      private static readonly SemaphoreSlim ModelDownloadLock = new(1, 1);

      private void EnsureModelDownloaded(string modelPath, ILogger logger)
      {
         if (File.Exists(modelPath)) return;

         ModelDownloadLock.Wait();

         try
         {
            // Re-check now that the lock is held, in case another thread already downloaded it.
            if (File.Exists(modelPath)) return;

            string filename = System.IO.Path.GetFileName(modelPath);
            string url = $"https://huggingface.co/ggerganov/whisper.cpp/resolve/main/{filename}";
            string? directory = System.IO.Path.GetDirectoryName(modelPath);
            string temporaryPath = modelPath + ".download";

            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

            logger.LogInformation("Downloading Whisper Model: {} -> {}", url, modelPath);

            var client = Services.GetRequiredService<IHttpClientFactory>().CreateClient();
            client.Timeout = TimeSpan.FromMilliseconds(Settings.CurrentValue.Transcription.TimeoutMilliseconds);

            using (var response = client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult())
            {
               response.EnsureSuccessStatusCode();

               using (var fileStream = File.Create(temporaryPath))
               {
                  response.Content.CopyToAsync(fileStream).GetAwaiter().GetResult();
               }
            }

            File.Move(temporaryPath, modelPath, overwrite: true);

            logger.LogInformation("Whisper Model Downloaded: {}", modelPath);
         }
         catch (Exception e)
         {
            logger.LogWarning("Failed To Download Whisper Model: {}, Because: {}", modelPath, e.Message);
         }
         finally
         {
            ModelDownloadLock.Release();
         }
      }

      private byte[]? ExtractAudio(string path)
      {
         string executable = System.IO.Path.Combine(Settings.CurrentValue.FFmpeg.Path ?? "", $"ffmpeg{Platform.Extension.Executable}");

         string[] arguments =
         [
            // Input file
            $"-i \"{path}\"",
            // Drop the video stream
            "-vn",
            // Downmix to mono
            "-ac 1",
            // Resample to 16kHz as expected by whisper.cpp
            "-ar 16000",
            // Encode as 16-bit PCM
            "-acodec pcm_s16le",
            // Overwrite output files
            "-y",
            // Output format: WAV
            "-f wav",
            // Output to stdout
            "-",
         ];

         return FFmpeg(executable, arguments, Settings.CurrentValue.FFmpeg.TimeoutMilliseconds, true, Logger);
      }

      // Image-based subtitle formats (DVD/Blu-ray) are bitmaps, not text - they can't be converted
      // to WebVTT, so they're not worth exposing as a track.
      private static readonly string[] UnsupportedSubtitleCodecs = ["dvd_subtitle", "hdmv_pgs_subtitle", "dvb_subtitle"];

      private List<EmbeddedSubtitle> GetEmbeddedSubtitles()
      {
         if (string.IsNullOrEmpty(FullPath)) return [];

         string executable = System.IO.Path.Combine(Settings.CurrentValue.FFmpeg.Path ?? "", $"ffprobe{Platform.Extension.Executable}");

         string[] arguments =
         [
            "-v quiet",
            "-print_format json",
            "-show_streams",
            "-select_streams s",
            $"\"{FullPath}\"",
         ];

         byte[]? output = FFmpeg(executable, arguments, Settings.CurrentValue.FFmpeg.TimeoutMilliseconds, true, Logger);

         if (output is null || output.Length == 0) return [];

         try
         {
            var result = JsonDocument.Parse(output).RootElement;
            var subtitles = new List<EmbeddedSubtitle>();

            if (result.TryGetProperty("streams", out var streamsElement))
            {
               foreach (var stream in streamsElement.EnumerateArray())
               {
                  int index = stream.GetProperty("index").GetInt32();
                  string codec = stream.TryGetProperty("codec_name", out var codecElement) ? codecElement.GetString() ?? "" : "";

                  if (UnsupportedSubtitleCodecs.Contains(codec)) continue;

                  string? language = stream.TryGetProperty("tags", out var tagsElement) &&
                                      tagsElement.TryGetProperty("language", out var languageElement)
                                      ? languageElement.GetString() : null;

                  subtitles.Add(new EmbeddedSubtitle(index, language, codec));
               }
            }

            return subtitles;
         }
         catch (Exception e)
         {
            Logger.LogDebug("Failed To Probe Embedded Subtitles For: {}, Because: {}", FullPath, e.Message);

            return [];
         }
      }

      /// <summary>
      /// Extracts and converts one of the video file's embedded subtitle streams to WebVTT, or null
      /// if extraction fails. <paramref name="index"/> is the absolute ffprobe stream index (as
      /// stored in <see cref="Subtitles"/>), not a subtitle-relative one.
      /// </summary>
      public string? GenerateEmbeddedSubtitle(int index)
      {
         if (string.IsNullOrEmpty(FullPath)) return null;

         string executable = System.IO.Path.Combine(Settings.CurrentValue.FFmpeg.Path ?? "", $"ffmpeg{Platform.Extension.Executable}");

         string[] arguments =
         [
            $"-i \"{FullPath}\"",
            $"-map 0:{index}",
            "-c:s webvtt",
            "-f webvtt",
            "-y",
            "-",
         ];

         byte[]? output = FFmpeg(executable, arguments, Settings.CurrentValue.FFmpeg.TimeoutMilliseconds, true, Logger);

         return (output is not null && output.Length > 0) ? Encoding.UTF8.GetString(output) : null;
      }

      private static string PlainTextFromVtt(string vtt)
      {
         var text = new List<string>();

         foreach (var line in vtt.Split('\n').Select(line => line.Trim()))
         {
            if (line.Length == 0) continue;
            if (line.StartsWith("WEBVTT", StringComparison.Ordinal)) continue;
            if (line.Contains("-->", StringComparison.Ordinal)) continue;
            if (line.All(char.IsDigit)) continue; // numeric cue identifier

            text.Add(line);
         }

         return string.Join(" ", text);
      }

      private void GenerateTranscriptFromEmbeddedSubtitles(List<EmbeddedSubtitle> subtitles)
      {
         Subtitles = JsonSerializer.Serialize(subtitles);
         TranscriptSegments = null;

         // Pick one stream to populate the searchable Transcript/TranscriptLanguage fields with:
         // the one matching the configured transcription language if there is one, else the first.
         string? language = Settings.CurrentValue.Transcription.Language;
         EmbeddedSubtitle primary = subtitles[0];

         if (!string.IsNullOrEmpty(language) && !string.Equals(language, "auto", StringComparison.OrdinalIgnoreCase))
         {
            foreach (var subtitle in subtitles)
            {
               if (!string.Equals(subtitle.Language, language, StringComparison.OrdinalIgnoreCase)) continue;

               primary = subtitle;

               break;
            }
         }

         string? vtt = GenerateEmbeddedSubtitle(primary.Index);

         Transcript = string.IsNullOrEmpty(vtt) ? "" : PlainTextFromVtt(vtt);
         TranscriptLanguage = primary.Language;
      }

      /// <summary>
      /// Overrides the DetectSubtitles() method of the MediaFile, checking for and using the video
      /// file's own embedded subtitle streams in place of a whisper.cpp transcript, if any exist.
      /// </summary>
      public override bool DetectSubtitles(bool force = false)
      {
         // Subtitles is "[]" (not null) once a video has been checked and found to have no embedded
         // streams, distinguishing "checked, none found" from "never checked" - the latter is what
         // Scanner.ForceDetectMissingSubtitles backfills for already-indexed videos, without forcing
         // a redundant re-probe of videos whose answer (either way) is already known.
         if (!force && Subtitles is not null) return Subtitles != "[]";

         var embedded = GetEmbeddedSubtitles();

         if (embedded.Count == 0)
         {
            Subtitles = "[]";

            return false;
         }

         GenerateTranscriptFromEmbeddedSubtitles(embedded);

         return true;
      }

      /// <summary>
      /// Overrides the GenerateTranscript() method of the MediaFile generating a speech transcript
      /// for the video file's audio using whisper.cpp. Only called for videos without embedded
      /// subtitle streams - see DetectSubtitles() above.
      /// </summary>
      public override void GenerateTranscript(bool force = false)
      {
         if (!Settings.CurrentValue.Transcription.Enabled) return;
         if (!force && !string.IsNullOrEmpty(Transcript)) return;
         if (string.IsNullOrEmpty(FullPath)) return;

         DirectoryInfo temp = Directory.CreateDirectory(System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName()));

         try
         {
            string audioPath = System.IO.Path.Combine(temp.FullName, "audio.wav");
            byte[]? audio = ExtractAudio(FullPath);

            if ((audio == null) || (audio.Length == 0))
            {
               Logger.LogDebug("No Audio Track Found For: {}", FullPath);

               Transcript = "";
               TranscriptLanguage = null;
               TranscriptSegments = null;

               return;
            }

            File.WriteAllBytes(audioPath, audio);

            EnsureModelDownloaded(Settings.CurrentValue.Transcription.Model, Logger);

            string executable = System.IO.Path.Combine(Settings.CurrentValue.Transcription.Path ?? "", $"whisper-cli{Platform.Extension.Executable}");
            string outputBase = System.IO.Path.Combine(temp.FullName, "output");

            string[] arguments =
            [
               // Path to the ggml model
               $"-m \"{Settings.CurrentValue.Transcription.Model}\"",
               // Language to transcribe in, or "auto" to detect
               $"-l {Settings.CurrentValue.Transcription.Language ?? "auto"}",
               // Write the transcript as JSON
               "-oj",
               // Output file path, without extension
               $"-of \"{outputBase}\"",
               // Input audio file
               $"\"{audioPath}\"",
            ];

            byte[]? output = FFmpeg(executable, arguments, Settings.CurrentValue.Transcription.TimeoutMilliseconds, true, Logger);

            if (output is not null && output.Length > 0)
            {
               Logger.LogTrace("{}", Encoding.UTF8.GetString(output));
            }

            string outputPath = outputBase + ".json";

            if (!File.Exists(outputPath))
            {
               Logger.LogWarning("Transcription Failed, No Output Produced For: {}", FullPath);

               Transcript = "";
               TranscriptLanguage = null;
               TranscriptSegments = null;

               return;
            }

            var result = JsonDocument.Parse(File.ReadAllText(outputPath)).RootElement;

            TranscriptLanguage = result.TryGetProperty("result", out var resultElement) &&
                                  resultElement.TryGetProperty("language", out var languageElement)
                                  ? languageElement.GetString() : null;

            var segments = new List<TranscriptSegment>();

            if (result.TryGetProperty("transcription", out var transcriptionElement))
            {
               foreach (var entry in transcriptionElement.EnumerateArray())
               {
                  string text = entry.GetProperty("text").GetString()?.Trim() ?? "";

                  if (text.Length == 0) continue;

                  // Whisper occasionally hallucinates a repetition loop (a character or short
                  // token repeated hundreds of times) on silence/music/noise. whisper.cpp's own
                  // entropy/logprob decoder-fail thresholds don't reliably catch this, so mirror
                  // OpenAI's reference implementation's compression-ratio check: a degenerate,
                  // highly-repetitive string compresses far better than natural language does.
                  if (IsRepetitive(text))
                  {
                     Logger.LogDebug("Repetitive Transcript Segment Discarded For: {}, Text: {}", FullPath, text);
                     continue;
                  }

                  long start = entry.GetProperty("offsets").GetProperty("from").GetInt64();
                  long end = entry.GetProperty("offsets").GetProperty("to").GetInt64();

                  segments.Add(new TranscriptSegment(start, end, text));
               }
            }

            segments = RemoveStuckSegments(segments);

            Transcript = string.Join(" ", segments.Select(segment => segment.Text));
            TranscriptSegments = segments.Count > 0 ? JsonSerializer.Serialize(segments) : null;
         }
         catch (Exception e)
         {
            Logger.LogWarning("Failed To Generate Transcript For: {}, Because: {}", FullPath, e.Message);

            Transcript = "";
            TranscriptLanguage = null;
            TranscriptSegments = null;
         }
         finally
         {
            temp.Delete(true);
         }
      }

      // Below this ratio the text is short/varied enough to be plausible speech. Above it, it's
      // almost certainly a hallucinated repetition loop. 2.4 matches the compression_ratio_threshold
      // OpenAI's reference Whisper implementation uses for the same purpose.
      private const double RepetitionCompressionRatioThreshold = 2.4;

      private static bool IsRepetitive(string text)
      {
         // Too short for the compression ratio to be meaningful; gzip's own overhead would dominate.
         if (text.Length < 50) return false;

         byte[] bytes = Encoding.UTF8.GetBytes(text);

         using MemoryStream compressed = new();

         using (GZipStream gzip = new(compressed, CompressionLevel.Optimal, leaveOpen: true))
         {
            gzip.Write(bytes, 0, bytes.Length);
         }

         return (double)bytes.Length / compressed.Length > RepetitionCompressionRatioThreshold;
      }

      // Whisper.cpp conditions each chunk's decode on the previously transcribed text. On long
      // stretches of silence/music/noise this occasionally makes the decoder get "stuck" repeating
      // the same short phrase verbatim across many disjoint segments (e.g. a name transcribed once
      // correctly, then echoed for the next several minutes) - too short internally to trip
      // IsRepetitive, but implausible for genuine speech to produce identically, over and over,
      // across unrelated time windows. A segment is dropped once its exact text accounts for both
      // a meaningful share and a minimum count of all segments.
      private const double StuckSegmentRatioThreshold = 0.2;
      private const int StuckSegmentMinimumCount = 4;

      private List<TranscriptSegment> RemoveStuckSegments(List<TranscriptSegment> segments)
      {
         if (segments.Count < StuckSegmentMinimumCount) return segments;

         var counts = segments.CountBy(segment => segment.Text)
                               .ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.OrdinalIgnoreCase);

         return segments.Where(segment =>
         {
            int count = counts[segment.Text];

            if ((count < StuckSegmentMinimumCount) || ((double)count / segments.Count < StuckSegmentRatioThreshold)) return true;

            Logger.LogDebug("Stuck Transcript Segment Discarded For: {}, Text: {}, Count: {}", FullPath, segment.Text, count);

            return false;
         }).ToList();
      }

      /// <summary>
      /// Builds a WebVTT subtitle track from the stored transcript segments, or null if there is
      /// no transcript (transcription disabled/pending/failed, or no audio track).
      /// </summary>
      public string? GenerateSubtitles()
      {
         if (string.IsNullOrEmpty(TranscriptSegments)) return null;

         var segments = JsonSerializer.Deserialize<List<TranscriptSegment>>(TranscriptSegments);

         if (segments is null || segments.Count == 0) return null;

         var content = new StringBuilder();

         content.AppendLine("WEBVTT");
         content.AppendLine();

         foreach (var segment in segments)
         {
            content.AppendLine($"{FormatTimestamp(segment.Start)} --> {FormatTimestamp(segment.End)}");
            content.AppendLine(segment.Text);
            content.AppendLine();
         }

         return content.ToString();
      }

      private static string FormatTimestamp(long milliseconds)
      {
         return TimeSpan.FromMilliseconds(milliseconds).ToString(@"hh\:mm\:ss\.fff");
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
         string? ecv = Settings.CurrentValue.FFmpeg.Encoder?.Video;
         string? eca = Settings.CurrentValue.FFmpeg.Encoder?.Audio;
         string? dcv = Settings.CurrentValue.FFmpeg.Decoder?.Video;
         string? dca = Settings.CurrentValue.FFmpeg.Decoder?.Audio;
         string? hwaccel = Settings.CurrentValue.FFmpeg.HardwareAcceleration;
         string executable = System.IO.Path.Combine(Settings.CurrentValue.FFmpeg.Path ?? "", $"ffmpeg{Platform.Extension.Executable}");
         DirectoryInfo temp = Directory.CreateDirectory(System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName()));
         string format = System.IO.Path.Combine(temp.FullName, "output-%05d.ts");

         string[] arguments =
         [
            // Add hardware acceleration method if enabled
            !string.IsNullOrEmpty(hwaccel) ? $"-hwaccel {hwaccel}" : "",
            // Set video decoder or fallback to libx264
            !string.IsNullOrEmpty(dcv) ? $"-c:v {dcv}" : "",
            // Set audio decoder or fallback to AAC
            !string.IsNullOrEmpty(dca) ? $"-c:a {dca}" : "",
            // Start time
            $"-ss {sequence * duration}",
            // Duration of 10 seconds
            (count == 0) ? $"-t {Duration - sequence * duration}" : $"-t {count * duration}",
            // Copy timestamps
            $"-copyts",
            // Input file
            $"-i \"{FullPath}\"",
            // Map all streams from the input
            $"-map 0",
            // Exclude all subtitle streams
            $"-map -0:s",
            // Set video encoder or fallback to libx264
            !string.IsNullOrEmpty(ecv) ? $"-c:v {ecv}" : $"-c:v libx264",
            // Set audio encoder or fallback to AAC
            !string.IsNullOrEmpty(eca) ? $"-c:a {eca}" : $"-c:a aac",
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
            arguments = [.. arguments, string.Format(template, settings.height, settings.bitrate, settings.maxrate, settings.bufsize)];
         }

         FFmpeg(executable, arguments, Settings.CurrentValue.FFmpeg.TimeoutMilliseconds, false, Logger);

         string filename = System.IO.Path.Combine(temp.FullName, "output-00000.ts");

         if (!File.Exists(filename))
         {
            throw new FileNotFoundException("Unable to find the generated segment: " + filename);
         }

         output = File.ReadAllBytes(filename);
         temp.Delete(true);

         return output;
      }

      #endregion // Video File Operations
   }
}
