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

using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration.Json;

namespace Arcadeia.Configuration
{
   public class WritableJsonConfigurationProvider(JsonConfigurationSource source) : JsonConfigurationProvider(source)
   {
      private readonly JsonConfigurationSource _source = source;
      private static readonly object _lock = new();

      public void Update(JsonObject updates)
      {
         var path = _source.Path;

         if (string.IsNullOrEmpty(path))
         {
            throw new ArgumentNullException(nameof(path), "Configuration file path cannot be null or empty.");
         }

         if (!File.Exists(path))
         {
            throw new FileNotFoundException($"Configuration file '{path}' does not exist.");
         }

         JsonObject json;

         try
         {
            // Read the existing JSON configuration
            json = JsonNode.Parse(File.ReadAllText(path))?.AsObject() ?? [];
         }
         catch (JsonException ex)
         {
            throw new InvalidOperationException($"The configuration file '{path}' contains invalid JSON.", ex);
         }

         lock (_lock)
         {
            // Recursively update the JSON structure
            Update(json, updates);

            // Write the updated JSON back to the file
            File.WriteAllText(path, json.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
         }
      }

      private static void Update(JsonObject target, JsonObject updates)
      {
         foreach (var (key, value) in updates)
         {
            if (value is JsonObject nestedObject)
            {
               // If the target key doesn't exist or isn't a JsonObject, create a new one
               if (!target.ContainsKey(key) || target[key] is not JsonObject targetNested)
               {
                  targetNested = [];
                  target[key] = targetNested;
               }

               // Recursively update the nested object
               Update(targetNested, nestedObject);
            }
            else if (value is JsonArray array)
            {
               target[key] = new JsonArray(array.Select(node => node?.DeepClone()).ToArray());
            }
            else
            {
               target[key] = value?.DeepClone();
            }
         }
      }
   }
}
