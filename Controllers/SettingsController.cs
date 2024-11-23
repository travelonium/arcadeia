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
      private readonly List<String> _serializableKeys =
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
         return Ok(Serialize(_configuration));
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
            var invalidKeys = configuration.Select(x => x.Key).Except(_serializableKeys).ToList();
            if (invalidKeys.Count != 0)
            {
               return BadRequest(new
               {
                  message = "One or more of the keys in the request are either invalid or not updatable",
                  detail = invalidKeys
               });
            }

            // Get the configuration root and the writable provider
            if (_configuration is not IConfigurationRoot configurationRoot)
            {
               return StatusCode(500, new { message = "Configuration root is not accessible" });
            }

            var provider = configurationRoot.Providers.OfType<WritableJsonConfigurationProvider>()
                                                      .Where(provider => provider?.Source?.Path != null && System.IO.File.Exists(provider.Source.Path))
                                                      .LastOrDefault();

            if (provider == null)
            {
               return StatusCode(500, new { message = "No writable JSON configuration providers are available" });
            }

            // Save the changes to the appropriate file
            provider.Save(configuration);

            return Ok();
         }
         catch (Exception ex)
         {
            _logger.LogError(ex, "Error updating settings");

            return StatusCode(500, new { message = "An error occurred while updating the settings", Error = ex.Message });
         }
      }

      private JsonNode? Serialize(IConfiguration configuration, int level = 0)
      {
         JsonObject result = [];

         foreach (var child in configuration.GetChildren())
         {
            if (level == 0 && !_serializableKeys.Contains(child.Key)) continue;

            if (child.Path.EndsWith(":0"))
            {
               var arr = new JsonArray();

               foreach (var arrayChild in configuration.GetChildren())
               {
                  arr.Add(Serialize(arrayChild, level + 1));
               }

               return arr;
            }
            else
            {
               result.Add(child.Key, Serialize(child, level + 1));
            }
         }

         if (result.Count == 0 && configuration is IConfigurationSection section)
         {
            if (bool.TryParse(section.Value, out bool boolean))
            {
               return JsonValue.Create(boolean);
            }
            else if (decimal.TryParse(section.Value, out decimal real))
            {
               return JsonValue.Create(real);
            }
            else if (long.TryParse(section.Value, out long integer))
            {
               return JsonValue.Create(integer);
            }

            return JsonValue.Create(section.Value);
         }

         return result;
      }
   }
}

