using System.Text.Json.Nodes;

namespace MediaCurator.Configuration
{
   public static class ConfigurationExtensions
   {
      public static JsonNode? ToJson(this IConfiguration configuration, IEnumerable<string>? whitelist = null)
      {
         JsonObject result = [];

         foreach (var child in configuration.GetChildren())
         {
            if (whitelist is not null && !whitelist.Contains(child.Key)) continue;

            if (child.Path.EndsWith(":0"))
            {
               var arr = new JsonArray();

               foreach (var arrayChild in configuration.GetChildren())
               {
                  arr.Add(ToJson(arrayChild, 1));
               }

               return arr;
            }
            else
            {
               result.Add(child.Key, ToJson(child, 1));
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

      private static JsonNode? ToJson(this IConfiguration configuration, int level = 0)
      {
         JsonObject result = [];

         foreach (var child in configuration.GetChildren())
         {
            if (child.Path.EndsWith(":0"))
            {
               var arr = new JsonArray();

               foreach (var arrayChild in configuration.GetChildren())
               {
                  arr.Add(ToJson(arrayChild, level + 1));
               }

               return arr;
            }
            else
            {
               result.Add(child.Key, ToJson(child, level + 1));
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