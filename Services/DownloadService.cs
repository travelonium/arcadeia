using System.Diagnostics;
using System.Text.RegularExpressions;

namespace MediaCurator.Services
{
   public partial class DownloadService(ILogger<DownloadService> logger, IConfiguration configuration) : IDownloadService
   {
      private readonly ILogger<DownloadService> _logger = logger;
      private readonly IConfiguration _configuration = configuration;

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

      public class FileAlreadyDownloadedException : Exception
      {
         public FileAlreadyDownloadedException() : base() { }

         public FileAlreadyDownloadedException(string message) : base(message) { }

         public FileAlreadyDownloadedException(string message, Exception innerException) : base(message, innerException) { }
      }

      public async Task<string?> GetMediaFileNameAsync(string url, string template = "%(title)s.%(ext)s")
      {
         if (string.IsNullOrWhiteSpace(url)) throw new ArgumentException("URL cannot be null or empty.", nameof(url));

         string executable = Path.Combine(_configuration["yt-dlp:Path"], $"yt-dlp{Platform.Extension.Executable}");

         if (!File.Exists(executable)) throw new FileNotFoundException($"yt-dlp executable not found at the specified path: {executable}");

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

         string executable = Path.Combine(_configuration["yt-dlp:Path"], $"yt-dlp{Platform.Extension.Executable}");

         if (!File.Exists(executable)) throw new FileNotFoundException($"yt-dlp executable not found at the specified path: {executable}");

         string options = _configuration.GetSection("yt-dlp:Options")?.Get<List<string>>()?.Aggregate((a, x) => $"{a} {x}") ?? string.Empty;

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
            Arguments = arguments.Aggregate((a, x) => $"{a} {x}"),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = false,
            UseShellExecute = false,
            CreateNoWindow = true
         };

         string fileName = string.Empty;
         TaskCompletionSource tcs = new();

         using Process process = new() { StartInfo = processStartInfo };
         process.OutputDataReceived += (sender, e) =>
         {
            try
            {
               if (!string.IsNullOrEmpty(e.Data))
               {
                  _logger.LogTrace(e.Data);

                  // extract and output the download progress
                  Match match = ProgressRegex().Match(e.Data);
                  if (match.Success)
                  {
                     if (double.TryParse(match.Groups[1].Value, out double percentage))
                     {
                        double value = percentage / 100.0;
                        progress?.Report($"Downloading: {value:0.000}\n");
                     }
                  }

                  // extract and output the destination file name
                  match = FileNameRegex().Match(e.Data);
                  if (match.Success)
                  {
                     fileName = match.Groups[1].Value;
                     progress?.Report($"Downloading: {fileName}\n");
                  }

                  // extract and output the merged file name
                  match = MergingFileNameRegex().Match(e.Data);
                  if (match.Success)
                  {
                     fileName = match.Groups[1].Value;
                     progress?.Report($"Merging: {fileName}\n");
                  }

                  // extract and output the moved file name
                  match = MovingFileNameRegex().Match(e.Data);
                  if (match.Success)
                  {
                     fileName = match.Groups[2].Value;
                     progress?.Report($"Moving: {fileName}\n");
                  }

                  // file already downloaded
                  match = AlreadyDownloadedRegex().Match(e.Data);
                  if (match.Success)
                  {
                     fileName = match.Groups[1].Value;
                     throw new FileAlreadyDownloadedException("Media file has already been downloaded.");
                  }
               }
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

         // Check if an exception was set and have the captured exception thrown
         if (tcs.Task.IsFaulted) await tcs.Task;

         if (process.ExitCode != 0)
         {
            _logger.LogError("Failed To Download URL: {Url}, Exist Code: {ExitCode}", url, process.ExitCode);

            if (!string.IsNullOrEmpty(error)) _logger.LogDebug("{}", error);

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

