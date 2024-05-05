using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace MediaCurator.Controllers
{
   [ApiController]
   [Route("[controller]")]
   public class SettingsController : Controller
   {
      private readonly IConfiguration _configuration;
      private readonly ILogger<MediaContainer> _logger;
      private readonly List<String> _serializableKeys = new()
      {
         "Thumbnails",
         "Streaming",
         "SupportedExtensions",
         "FFmpeg",
         "Solr",
         "Scanner",
         "Mounts", 
      };

      public SettingsController(ILogger<MediaContainer> logger,
                                     IConfiguration configuration)
      {
         _logger = logger;
         _configuration = configuration;
      }

      // GET: /<controller>/appsettings.json
      [HttpGet]
      [Route("appsettings.json")]
      [Produces("application/json")]
      public IActionResult Index()
      {
         // return Ok(_configuration.AsEnumerable().ToDictionary(k => k.Key, v => v.Value));
         return Ok(Serialize(_configuration));
      }

      private JsonNode? Serialize(IConfiguration config, int level = 0)
      {
         JsonObject result = new();

         foreach (var child in config.GetChildren())
         {
            if (level == 0 && !_serializableKeys.Contains(child.Key)) continue;

            if (child.Path.EndsWith(":0"))
            {
               var arr = new JsonArray();

               foreach (var arrayChild in config.GetChildren())
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

         if (result.Count == 0 && config is IConfigurationSection section)
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

