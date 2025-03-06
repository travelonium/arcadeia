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

using System.Diagnostics;
using Arcadeia.Configuration;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;

namespace Arcadeia.Services
{
   public partial class DownloadService(IOptionsMonitor<Settings> settings, ILogger<DownloadService> logger) : IDownloadService
   {
      private readonly ILogger<DownloadService> _logger = logger;

      private readonly IOptionsMonitor<Settings> _settings = settings;

      private readonly ConcurrentDictionary<string, bool> _downloading = new();

      [GeneratedRegex(@"\[download\]\s+([\d\.]+)% of")]
      private static partial Regex ProgressRegex();

      [GeneratedRegex(@"\[download\]\s+Destination:\s+(.+)")]
      private static partial Regex FileNameRegex();

      [GeneratedRegex(@"\[Merger\] Merging formats into \"".*\/(.+)\""")]
      private static partial Regex MergingFileNameRegex();

      [GeneratedRegex(@"\[MoveFiles\] Moving file \"".*\/(.+)\"" to \"".*\/(.+)\""")]
      private static partial Regex MovingFileNameRegex();

      [GeneratedRegex(@"\[download\] .*\/(.+) has already been downloaded")]
      private static partial Regex AlreadyDownloadedRegex();

      [GeneratedRegex(@"ERROR: \[\w+\]\s+(.+)")]
      private static partial Regex ErrorRegex();

      public class FileAlreadyDownloadedException : Exception
      {
         public FileAlreadyDownloadedException() : base() { }

         public FileAlreadyDownloadedException(string message) : base(message) { }

         public FileAlreadyDownloadedException(string message, Exception innerException) : base(message, innerException) { }
      }

      public bool Downloading(string url, string path)
      {
         return _downloading.ContainsKey(path + url);
      }

      public async Task<string?> GetMediaFileNameAsync(string url, string template = "%(title)s.%(ext)s")
      {
         if (string.IsNullOrWhiteSpace(url)) throw new ArgumentException("URL cannot be null or empty.", nameof(url));

         string executable = Path.Combine(_settings.CurrentValue.YtDlp.Path ?? "", $"yt-dlp{Platform.Extension.Executable}");

         ProcessStartInfo processStartInfo = new()
         {
            FileName = executable,
            Arguments = $"--get-filename -o \"{template}\" \"{url}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
         };

         using Process process = new() { StartInfo = processStartInfo };
         process.Start();

         _logger.LogTrace("{FileName} {Arguments}", process.StartInfo.FileName, process.StartInfo.Arguments);

         string output = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
         string error = await process.StandardError.ReadToEndAsync().ConfigureAwait(false);

         await process.WaitForExitAsync().ConfigureAwait(false);

         if (process.ExitCode != 0)
         {
            _logger.LogError("Failed To Determine File Name For: {}, Exist Code: {}", url, process.ExitCode);

            if (!string.IsNullOrEmpty(error)) _logger.LogDebug("{}", error);

            return null;
         }

         return output.Trim();
      }

      public async Task<string?> DownloadMediaFileAsync(string url, string path, IProgress<string>? progress = null, string template = "%(title)s.%(ext)s", bool overwrite = false)
      {
         if (string.IsNullOrWhiteSpace(url)) throw new ArgumentException("URL cannot be null or empty.", nameof(url));
         if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Path cannot be null or empty.", nameof(path));

         string executable = Path.Combine(_settings.CurrentValue.YtDlp.Path ?? "", $"yt-dlp{Platform.Extension.Executable}");

         string options = string.Join(" ", _settings.CurrentValue.YtDlp.Options.Where(x => !string.IsNullOrEmpty(x)).ToArray());

         string[] arguments = [
            $"-o \"{template}\"",
            $"--paths \"{path}\"",
            $"--paths \"temp:/tmp\"",
            overwrite ? "--force-overwrites" : "",
            $"--fixup force",
            $"{options}",
            $"\"{url}\""
         ];

         ProcessStartInfo processStartInfo = new()
         {
            FileName = executable,
            Arguments = string.Join(" ", arguments.Where(x => !string.IsNullOrEmpty(x)).ToArray()),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = false,
            UseShellExecute = false,
            CreateNoWindow = true
         };

         string fileName = string.Empty;
         TaskCompletionSource tcs = new();

         using Process process = new() { StartInfo = processStartInfo };

         _downloading.TryAdd(path + url, true);

         string? ProcessOutput(string? data)
         {
            string? fileName = null;

            if (string.IsNullOrEmpty(data)) return fileName;

            _logger.LogTrace("{}", data);

            Match match = ProgressRegex().Match(data);
            if (match.Success && double.TryParse(match.Groups[1].Value, out double percentage))
            {
               progress?.Report($"Downloading: {percentage / 100.0:0.000}\n");
            }
            else if ((match = FileNameRegex().Match(data)).Success)
            {
               fileName = match.Groups[1].Value;
               progress?.Report($"Downloading: {fileName}\n");
            }
            else if ((match = MergingFileNameRegex().Match(data)).Success)
            {
               fileName = match.Groups[1].Value;
               progress?.Report($"Merging: {fileName}\n");
            }
            else if ((match = MovingFileNameRegex().Match(data)).Success)
            {
               fileName = match.Groups[2].Value;
               progress?.Report($"Moving: {fileName}\n");
            }
            else if ((match = ErrorRegex().Match(data)).Success)
            {
               string error = match.Groups[1].Value;
               progress?.Report($"Error: {error}\n");
            }
            else if ((match = AlreadyDownloadedRegex().Match(data)).Success)
            {
               fileName = match.Groups[1].Value;
               throw new FileAlreadyDownloadedException("Media file has already been downloaded.");
            }

            return fileName;
         }

         process.OutputDataReceived += (sender, e) =>
         {
            try
            {
               fileName = ProcessOutput(e.Data) ?? fileName;
            }
            catch (Exception ex)
            {
               tcs.TrySetException(ex);
            }
         };

         _logger.LogTrace("{FileName} {Arguments}", process.StartInfo.FileName, process.StartInfo.Arguments);

         process.Start();
         process.BeginOutputReadLine();

         string error = await process.StandardError.ReadToEndAsync().ConfigureAwait(false);
         await process.WaitForExitAsync().ConfigureAwait(false);

         _downloading.TryRemove(path + url, out _);

         // Check if an exception was set and have the captured exception thrown
         if (tcs.Task.IsFaulted) await tcs.Task;

         if (process.ExitCode != 0)
         {
            _logger.LogError("Failed To Download URL: {Url}, Exist Code: {ExitCode}", url, process.ExitCode);

            if (!string.IsNullOrEmpty(error))
            {
               _logger.LogDebug("{}", error);

               foreach (var line in error.Split('\n', StringSplitOptions.RemoveEmptyEntries))
               {
                  ProcessOutput(line);
               }
            }

            return null;
         }

         return Path.Combine(path, fileName);
      }

      public Task StartAsync(CancellationToken cancellationToken)
      {
         _logger.LogInformation("Download Service Started.");

         return Task.CompletedTask;
      }

      public Task StopAsync(CancellationToken cancellationToken)
      {
         _logger.LogInformation("Download Service Stopped.");

         return Task.CompletedTask;
      }
   }
}

