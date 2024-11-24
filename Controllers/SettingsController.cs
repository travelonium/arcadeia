using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using MediaCurator.Configuration;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace MediaCurator.Controllers
{
   [ApiController]
   [Route("[controller]")]
   public class SettingsController(ILogger<MediaContainer> logger,
                                   IConfiguration configuration) : Controller
   {
      private readonly IConfiguration _configuration = configuration;
      private readonly ILogger<MediaContainer> _logger = logger;
      private readonly string[] _whitelist =
      [
         "Thumbnails",
         "Streaming",
         "SupportedExtensions",
         "FFmpeg",
         "yt-dlp",
         "Solr",
         "Scanner",
         "Mounts",
      ];

      // GET: /settings
      [HttpGet]
      [Produces("application/json")]
      public IActionResult Get()
      {
         return Ok(_configuration.ToJson(_whitelist));
      }

      // GET: /settings
      [HttpPost]
      [Produces("application/json")]
      [Consumes("application/json")]
      public IActionResult Post([FromBody] JsonObject configuration)
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

