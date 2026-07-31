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

using System.Reflection;
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
                  arr.Add(ToJson(arrayChild, GetElementType(typeof(Settings))));
               }

               return arr;
            }
            else
            {
               result.Add(child.Key, ToJson(child, GetPropertyType(typeof(Settings), child.Key)));
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

      // Note: an IConfiguration section with no children is ambiguous - it's produced both by an
      // empty JSON array (e.g. "Mounts": []) and an empty JSON object (e.g. "Foo": {}), since
      // Microsoft.Extensions.Configuration.Json flattens both to the same "no children, null value"
      // shape. That ambiguity can't be resolved from IConfiguration alone, so the corresponding CLR
      // property's type (walked down from the root Settings type alongside the configuration tree) is
      // used here to tell an empty list apart from an empty object when re-serializing to JSON.
      private static JsonNode? ToJson(this IConfiguration configuration, Type? type)
      {
         JsonObject result = [];

         foreach (var child in configuration.GetChildren())
         {
            if (child.Path.EndsWith(":0"))
            {
               var arr = new JsonArray();
               var elementType = GetElementType(type);

               foreach (var arrayChild in configuration.GetChildren())
               {
                  arr.Add(ToJson(arrayChild, elementType));
               }

               return arr;
            }
            else
            {
               result.Add(child.Key, ToJson(child, GetPropertyType(type, child.Key)));
            }
         }

         if (result.Count == 0)
         {
            if (IsArrayLike(type)) return new JsonArray();
            if (IsDictionaryLike(type)) return new JsonObject();

            if (configuration is IConfigurationSection section)
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
         }

         return result;
      }

      private static Type? GetPropertyType(Type? type, string name)
      {
         return type?.GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)?.PropertyType;
      }

      private static Type? GetElementType(Type? type)
      {
         if (type is null) return null;

         var enumerable = type.GetInterfaces().Prepend(type)
                               .FirstOrDefault(candidate => candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(IEnumerable<>));

         return enumerable?.GetGenericArguments().FirstOrDefault();
      }

      // A List<T>-shaped section with no children should round-trip as a JSON array ("[]"). A
      // Dictionary<TKey,TValue>-shaped section with no children should round-trip as a JSON object
      // ("{}") instead, since its children are named entries rather than positional ones.
      private static bool IsArrayLike(Type? type)
      {
         if (type is null || type == typeof(string)) return false;
         if (IsDictionaryLike(type)) return false;

         return typeof(System.Collections.IEnumerable).IsAssignableFrom(type);
      }

      private static bool IsDictionaryLike(Type? type)
      {
         return type is not null && typeof(System.Collections.IDictionary).IsAssignableFrom(type);
      }
   }
}
