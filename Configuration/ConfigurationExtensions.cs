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

using System.Text.Json.Nodes;

namespace Arcadeia.Configuration
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