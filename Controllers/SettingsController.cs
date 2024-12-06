using MediaCurator.Services;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MediaCurator.Configuration;
using System.Text.Json;
using System.Text.RegularExpressions;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace MediaCurator.Controllers
{
   [ApiController]
   [Route("api/[controller]")]
   public partial class SettingsController(IConfiguration configuration,
                                           IScannerService _scannerService,
                                           ILogger<SettingsController> _logger,
                                           IOptionsMonitor<Settings> _settings,
                                           IFileSystemService fileSystemService,
                                           IHostApplicationLifetime applicationLifetime) : Controller
   {
      private readonly IConfigurationRoot _configuration = (IConfigurationRoot)configuration;
      private readonly CancellationToken _cancellationToken = applicationLifetime.ApplicationStopping;

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
      ];

      // GET: /api/settings
      [HttpGet]
      [Produces("application/json")]
      public async Task<IActionResult> Get()
      {
         // Manually reload the configuration to ensure that recent changes are reflected
         await Task.Run(() => _configuration.Reload());

         var json = (await Task.Run(() => _configuration.ToJson(_whitelist))) as JsonObject;

         if (json is not null)
         {
            json["System"] = new JsonObject
            {
               // Add the number of logical processors
               ["ProcessorCount"] = Environment.ProcessorCount,

               ["FFmpeg"] = new JsonObject
               {
                  // Add supported hardware acceleration methods
                  ["HardwareAcceleration"] = JsonNode.Parse(JsonSerializer.Serialize(HardwareAccelerators())),

                  // Add the supported codecs
                  ["Codecs"] = JsonNode.Parse(JsonSerializer.Serialize(Codecs())),
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
                     var mount = _settings.CurrentValue.Mounts.FirstOrDefault(mount => mount.Folder == item?["Folder"]?.ToString());
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
               scanner["Scanning"] = _scannerService.Scanning;
               scanner["Updating"] = _scannerService.Updating;
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
            if (_configuration is not IConfigurationRoot configurationRoot)
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
            await Task.Run(() => _configuration.Reload());

            // Restart the _scannerService if any changes have been made to the Scanner.
            if (configuration.ContainsKey("Scanner"))
            {
               await _scannerService.RestartAsync(_cancellationToken);
            }

            // Restart the FileSystemService if any changes have been made to the Mounts.
            if (configuration.ContainsKey("Mounts"))
            {
               await fileSystemService.RestartAsync(_cancellationToken);
            }

            return Ok();
         }
         catch (Exception ex)
         {
            _logger.LogError(ex, "Error Updating Settings, Because: {}", ex.Message);

            return StatusCode(500, new { message = "An error occurred while updating the settings.", error = ex.Message });
         }
      }

      private Codecs? Codecs()
      {
         string executable = System.IO.Path.Combine(_settings.CurrentValue.FFmpeg.Path, $"ffmpeg{Platform.Extension.Executable}");

         string[] arguments =
         [
            "-encoders"
         ];

         var encoders = VideoFile.FFmpeg(executable, arguments, _settings.CurrentValue.FFmpeg.TimeoutMilliseconds, true, _logger);

         arguments =
         [
            "-decoders"
         ];

         var decoders = VideoFile.FFmpeg(executable, arguments, _settings.CurrentValue.FFmpeg.TimeoutMilliseconds, true, _logger);

         if (encoders is not null && decoders is not null)
         {
            return new Codecs(System.Text.Encoding.UTF8.GetString(encoders), System.Text.Encoding.UTF8.GetString(decoders));
         }

         return null;
      }

      private IEnumerable<string> HardwareAccelerators()
      {
         string executable = System.IO.Path.Combine(_settings.CurrentValue.FFmpeg.Path, $"ffmpeg{Platform.Extension.Executable}");

         string[] arguments =
         [
            "-hwaccels"
         ];

         var result = VideoFile.FFmpeg(executable, arguments, _settings.CurrentValue.FFmpeg.TimeoutMilliseconds, true, _logger);

         if (result != null)
         {
            return HardwareAcceleratorsRegex().Matches(System.Text.Encoding.UTF8.GetString(result))
                                              .Select(x => x.Groups[1].Value.Trim());
         }

         return [];
      }
   }
}

