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

using Arcadeia.Services;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Arcadeia.Configuration;
using System.Text.Json;
using System.Text.RegularExpressions;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Arcadeia.Controllers
{
   [ApiController]
   [Route("api/[controller]")]
   public partial class SettingsController : Controller
   {
      private readonly IConfigurationRoot configuration;
      private readonly CancellationToken cancellationToken;
      private readonly IScannerService scannerService;
      private readonly ILogger<SettingsController> logger;
      private readonly IOptionsMonitor<Settings> settings;
      private readonly IFileSystemService fileSystemService;
      private static Lazy<string[]> hardwareAccelerators = new();
      private static Lazy<Codecs?> codecs = new();

      [GeneratedRegex(@"^([\w]+)$", RegexOptions.Multiline)]
      private static partial Regex HardwareAcceleratorsRegex();

      private readonly string[] _whitelist =
      [
         "Thumbnails",
         "Streaming",
         "SupportedExtensions",
         "FFmpeg",
         "YtDlp",
         "Solr",
         "Scanner",
         "Mounts",
         "Security"
      ];
      public SettingsController(IConfiguration configuration,
                                              IScannerService scannerService,
                                              ILogger<SettingsController> logger,
                                              IOptionsMonitor<Settings> settings,
                                              IFileSystemService fileSystemService,
                                              IHostApplicationLifetime applicationLifetime)
      {
         this.scannerService = scannerService;
         this.logger = logger;
         this.settings = settings;
         this.fileSystemService = fileSystemService;
         this.configuration = (IConfigurationRoot)configuration;
         cancellationToken = applicationLifetime.ApplicationStopping;
         if (!hardwareAccelerators.IsValueCreated) hardwareAccelerators = new Lazy<string[]>([.. HardwareAccelerators()]);
         if (!codecs.IsValueCreated) codecs = new Lazy<Codecs?>(Codecs());
      }

      // GET: /api/settings
      [HttpGet]
      [Produces("application/json")]
      public async Task<IActionResult> Get()
      {
         // Manually reload the configuration to ensure that recent changes are reflected
         await Task.Run(() => configuration.Reload());

         var json = (await Task.Run(() => configuration.ToJson(_whitelist))) as JsonObject;

         if (json is not null)
         {
            json["System"] = new JsonObject
            {
               // Add the number of logical processors
               ["ProcessorCount"] = Environment.ProcessorCount,

               ["FFmpeg"] = new JsonObject
               {
                  // Add supported hardware acceleration methods
                  ["HardwareAcceleration"] = JsonNode.Parse(JsonSerializer.Serialize(hardwareAccelerators.Value)),

                  // Add the supported codecs
                  ["Codecs"] = JsonNode.Parse(JsonSerializer.Serialize(codecs.Value)),
               },
            };

            // Add an Available key for each mount
            var mounts = json["Mounts"]?.AsArray();
            if (mounts is not null)
            {
               foreach (var item in mounts)
               {
                  try
                  {
                     if (item is null) continue;
                     var mount = settings.CurrentValue.Mounts.FirstOrDefault(mount => mount.Folder == item?["Folder"]?.ToString());
                     if (mount is null) continue;

                     // Mask the password in Options
                     /*
                     if (!string.IsNullOrEmpty(mount.Options))
                     {
                        var options = mount.Options.Split(',');
                        for (int i = 0; i < options.Length; i++)
                        {
                           if (options[i].StartsWith("password=", StringComparison.OrdinalIgnoreCase))
                           {
                              var parts = options[i].Split('=');
                              if (parts.Length == 2)
                              {
                                 options[i] = $"password={new string('*', parts[1].Length)}";
                              }
                           }
                        }
                        item["Options"] = string.Join(",", options);
                     }
                     */

                     var fileSystemMount = fileSystemService.Mounts.Where(x => x.Folder == mount.Folder).FirstOrDefault();
                     if (fileSystemMount is not null)
                     {
                        item["Error"] = fileSystemMount.Error;
                        item["Available"] = fileSystemMount.Available;
                     }
                  }
                  catch (Exception)
                  {
                     continue;
                  }
               }
            }

            // Add Scanning and Updating keys to Scanner
            var scanner = json["Scanner"];
            if (scanner is not null)
            {
               scanner["Scanning"] = scannerService.Scanning;
               scanner["Updating"] = scannerService.Updating;
            }
         }
         else
         {
            return StatusCode(500, new { message = "Failed to parse the configuration." });
         }

         return Ok(json);
      }

      // POST: /api/settings
      [HttpPost]
      [Produces("application/json")]
      [Consumes("application/json")]
      public async Task<IActionResult> Post([FromBody] JsonObject configuration)
      {
         if (settings.CurrentValue.Security.Settings.ReadOnly)
         {
            return StatusCode(401, new { message = "The settings are read-only." });
         }

         try
         {
            // Validate top-level keys against the allowed list
            var invalidKeys = configuration.Select(x => x.Key).Except(_whitelist).ToList();
            if (invalidKeys.Count != 0)
            {
               return BadRequest(new
               {
                  message = "One or more of the keys in the request are either invalid or non-updatable.",
                  detail = invalidKeys
               });
            }

            // Get the configuration root and the writable provider
            if (this.configuration is not IConfigurationRoot configurationRoot)
            {
               return StatusCode(500, new { message = "Configuration root is not accessible." });
            }

            // Select the appsettings.{environment}.json configuration file
            var provider = configurationRoot.Providers.OfType<WritableJsonConfigurationProvider>()
                                                      .Where(provider => provider?.Source?.Path != null && System.IO.File.Exists(provider.Source.Path))
                                                      .LastOrDefault();

            if (provider == null)
            {
               return StatusCode(500, new { message = "No writable JSON configuration providers are available." });
            }

            // Save the changes to the appropriate file
            provider.Update(configuration);

            // Manually reload the configuration to ensure that recent changes are reflected
            await Task.Run(() => this.configuration.Reload());

            // Restart the _scannerService if any changes have been made to the Scanner.
            if (configuration.ContainsKey("Scanner"))
            {
               await scannerService.RestartAsync(cancellationToken);
            }

            // Restart the FileSystemService if any changes have been made to the Mounts.
            if (configuration.ContainsKey("Mounts"))
            {
               await fileSystemService.RestartAsync(cancellationToken);
            }

            return Ok();
         }
         catch (Exception ex)
         {
            logger.LogError(ex, "Error Updating Settings, Because: {}", ex.Message);

            return StatusCode(500, new { message = "An error occurred while updating the settings.", error = ex.Message });
         }
      }

      private Codecs? Codecs()
      {
         string executable = System.IO.Path.Combine(settings.CurrentValue.FFmpeg.Path ?? "", $"ffmpeg{Platform.Extension.Executable}");

         string[] arguments =
         [
            "-encoders"
         ];

         var encoders = VideoFile.FFmpeg(executable, arguments, settings.CurrentValue.FFmpeg.TimeoutMilliseconds, true, logger);

         arguments =
         [
            "-decoders"
         ];

         var decoders = VideoFile.FFmpeg(executable, arguments, settings.CurrentValue.FFmpeg.TimeoutMilliseconds, true, logger);

         if (encoders is not null && decoders is not null)
         {
            return new Codecs(System.Text.Encoding.UTF8.GetString(encoders), System.Text.Encoding.UTF8.GetString(decoders));
         }

         return null;
      }

      private IEnumerable<string> HardwareAccelerators()
      {
         string executable = System.IO.Path.Combine(settings.CurrentValue.FFmpeg.Path ?? "", $"ffmpeg{Platform.Extension.Executable}");

         string[] arguments =
         [
            "-hwaccels"
         ];

         var result = VideoFile.FFmpeg(executable, arguments, settings.CurrentValue.FFmpeg.TimeoutMilliseconds, true, logger);

         if (result != null)
         {
            return HardwareAcceleratorsRegex().Matches(System.Text.Encoding.UTF8.GetString(result))
                                              .Select(x => x.Groups[1].Value.Trim());
         }

         return [];
      }
   }
}

