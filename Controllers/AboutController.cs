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
using System.Reflection;
using Arcadeia.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;

namespace Arcadeia.Controllers
{
   [ApiController]
   [Route("api/[controller]")]
   public partial class AboutController(ILogger<AboutController> logger, IOptionsMonitor<Settings> settings) : Controller
   {
      private const int TimeoutMilliseconds = 15000;

      private static readonly Assembly assembly = typeof(AboutController).Assembly;

      [GeneratedRegex(@"version\s+v?(\S+)", RegexOptions.IgnoreCase)]
      private static partial Regex LabelledVersionRegex();

      [GeneratedRegex(@"v?(\d+(?:\.\d+)+\S*)")]
      private static partial Regex NumericVersionRegex();

      // GET: /api/about
      [HttpGet]
      [Produces("application/json")]
      public async Task<IActionResult> Get()
      {
         // the informational version looks like 1.1.0 or 1.1.1-alpha.0.4 followed by +<commit hash>
         var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "";
         var (version, commit) = informationalVersion.Split('+', 2) switch
         {
            [var v, var c] => (v, c),
            [var v] => (v, null),
            _ => (null, null),
         };

         var metadata = assembly.GetCustomAttributes<AssemblyMetadataAttribute>().ToDictionary(x => x.Key, x => x.Value);

         var ffmpeg = Path.Combine(settings.CurrentValue.FFmpeg.Path ?? "", $"ffmpeg{Platform.Extension.Executable}");
         var ytDlp = Path.Combine(settings.CurrentValue.YtDlp.Path ?? "", $"yt-dlp{Platform.Extension.Executable}");
         var whisper = Path.Combine(settings.CurrentValue.Transcription.Path ?? "", $"whisper-cli{Platform.Extension.Executable}");

         var components = await Task.WhenAll(
            ComponentVersion("FFmpeg", ffmpeg, "-version"),
            ComponentVersion("yt-dlp", ytDlp, "--version"),
            ComponentVersion("whisper.cpp", whisper, "--version")
         );

         return Ok(new
         {
            Name = assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product,
            Version = version,
            Commit = commit,
            CommitDate = metadata.GetValueOrDefault("CommitDate"),
            Copyright = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright,
            License = "AGPL-3.0-or-later",
            Repository = metadata.GetValueOrDefault("RepositoryUrl"),
            Runtime = RuntimeInformation.FrameworkDescription,
            OperatingSystem = RuntimeInformation.OSDescription,
            Architecture = RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant(),
            Components = components.ToDictionary(x => x.Name, x => x.Version),
         });
      }

      private async Task<(string Name, string? Version)> ComponentVersion(string name, string executable, string argument)
      {
         try
         {
            using Process process = new()
            {
               StartInfo = new ProcessStartInfo
               {
                  FileName = executable,
                  Arguments = argument,
                  RedirectStandardOutput = true,
                  RedirectStandardError = true,
                  UseShellExecute = false,
                  CreateNoWindow = true
               }
            };

            process.Start();

            using var cancellation = new CancellationTokenSource(TimeoutMilliseconds);
            var output = process.StandardOutput.ReadToEndAsync(cancellation.Token);
            var error = process.StandardError.ReadToEndAsync(cancellation.Token);

            try
            {
               await process.WaitForExitAsync(cancellation.Token);
            }
            catch (OperationCanceledException)
            {
               process.Kill(true);
               logger.LogWarning("{Name} Version Check Timeout: {FileName} {Arguments}", name, executable, argument);
               return (name, null);
            }

            // the standard error may hold diagnostics with numbers of their own, so it's only a fallback
            return (name, ParseVersion(await output) ?? ParseVersion(await error));
         }
         catch (Exception e)
         {
            logger.LogDebug("{Name} Version Check Failed: {FileName} {Arguments}, Because: {Message}", name, executable, argument, e.Message);
            return (name, null);
         }
      }

      public static string? ParseVersion(string output)
      {
         foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
         {
            var match = LabelledVersionRegex().Match(line);
            if (!match.Success) match = NumericVersionRegex().Match(line);
            if (match.Success) return match.Groups[1].Value;
         }

         return null;
      }
   }
}
