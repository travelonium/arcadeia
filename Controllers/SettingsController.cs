using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using MediaCurator.Configuration;
using System.Formats.Asn1;
using MediaCurator.Services;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace MediaCurator.Controllers
{
   [ApiController]
   [Route("api/[controller]")]
   public class SettingsController(ILogger<SettingsController> logger,
                                   IServiceProvider services,
                                   IHostApplicationLifetime applicationLifetime,
                                   IConfiguration configuration) : Controller
   {
      private readonly IConfigurationRoot _configuration = (IConfigurationRoot)configuration;
      private readonly IServiceProvider _services = services;
      private readonly ILogger<SettingsController> _logger = logger;
      private readonly CancellationToken _cancellationToken = applicationLifetime.ApplicationStopping;
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

         var json = await Task.Run(() => _configuration.ToJson(_whitelist));

         // Add the number of logical processors as Scanner:MaximumParallelScannerTasks
         if (json is not null)
         {
            var scanner = json["Scanner"];
            if (scanner is not null) scanner["MaximumParallelScannerTasks"] = Environment.ProcessorCount;
            // TODO: Add Scanning: bool and Updating: bool keys to the response.
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

            // Restart the ScannerService if any changes have been made to the Scanner.
            if (configuration.ContainsKey("Scanner"))
            {
               var scannerService = _services.GetService<IScannerService>();
               if (scannerService != null)
               {
                  await scannerService.RestartAsync(_cancellationToken);
               }
            }

            // Restart the FileSystemService if any changes have been made to the Mounts.
            if (configuration.ContainsKey("Mounts"))
            {
               var fileSystemService = _services.GetService<IFileSystemService>();
               if (fileSystemService != null)
               {
                  await fileSystemService.RestartAsync(_cancellationToken);
               }
            }

            return Ok();
         }
         catch (Exception ex)
         {
            _logger.LogError(ex, "Error Updating Settings, Because: {}", ex.Message);

            return StatusCode(500, new { message = "An error occurred while updating the settings.", error = ex.Message });
         }
      }
   }
}

