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
using SolrNet.Attributes;

namespace Arcadeia.Models
{
   public class MediaContainer : IEquatable<MediaContainer>
   {
      [SolrUniqueKey("id")]
      public string? Id { get; set; }

      [SolrField("name")]
      public string? Name { get; set; }

      [SolrField("type")]
      public string? Type { get; set; }

      [SolrUniqueKey("parent")]
      public string? Parent { get; set; }

      [SolrUniqueKey("parentType")]
      public string? ParentType { get; set; }

      [SolrUniqueKey("parents")]
      public string[]? Parents { get; set; }

      [SolrField("description")]
      public string? Description { get; set; }

      [SolrField("path")]
      public string? Path { get; set; }

      [SolrField("fullPath")]
      public string? FullPath { get; set; }

      [SolrField("size")]
      public long Size { get; set; }

      [SolrField("checksum")]
      public string? Checksum { get; set; }

      [SolrField("contentType")]
      public string? ContentType { get; set; }

      [SolrField("extension")]
      public string? Extension { get; set; }

      [SolrField("views")]
      public long Views { get; set; }

      [SolrField("dateAccessed")]
      public DateTime? DateAccessed { get; set; }

      [SolrField("dateAdded")]
      public DateTime? DateAdded { get; set; }

      [SolrField("dateCreated")]
      public DateTime? DateCreated { get; set; }

      [SolrField("dateModified")]
      public DateTime? DateModified { get; set; }

      [SolrField("dateTaken")]
      public DateTime? DateTaken { get; set; }

      [SolrField("thumbnails")]
      public int Thumbnails { get; set; }

      [SolrField("duration")]
      public double? Duration { get; set; }

      [SolrField("width")]
      public long Width { get; set; }

      [SolrField("height")]
      public long Height { get; set; }

      [SolrField("flags")]
      public string[]? Flags { get; set; }

      private static bool Compare(object? left, object? right)
      {
         if (left is DateTime dtl && right is DateTime dtr)
         {
            return dtl.TruncateToSeconds() == dtr.TruncateToSeconds();
         }
         if (left is string[] stringArrayLeft && right is string[] stringArrayRight)
         {
            return SequenceEquals(stringArrayLeft, stringArrayRight);
         }
         if ((left is string[] emptyArrayLeft && right is null && emptyArrayLeft.Length == 0) ||
             (left is null && right is string[] emptyArrayRight && emptyArrayRight.Length == 0))
         {
            return true; // null and empty arrays are considered equal
         }
         if (left is Array arrayLeft && right is Array arrayRight)
         {
            return arrayLeft.Cast<object>().SequenceEqual(arrayRight.Cast<object>());
         }

         return Equals(left, right);
      }

      public bool Equals(MediaContainer? other)
      {
         if (other is null) return false;
         if (ReferenceEquals(this, other)) return true;

         return Properties().All(property =>
         {
            var value1 = property.GetValue(this);
            var value2 = property.GetValue(other);

            return Compare(value1, value2);
         });
      }

      public override bool Equals(object? obj) => obj is MediaContainer other && Equals(other);

      public override int GetHashCode()
      {
         var hash = new HashCode();

         foreach (var property in Properties())
         {
            hash.Add(property.GetValue(this));
         }

         return hash.ToHashCode();
      }

      private static bool SequenceEquals(string[]? left, string[]? right)
      {
         if (ReferenceEquals(left, right)) return true;

         // consider null and empty sequences as equal
         if ((left == null || left.Length == 0) && (right == null || right.Length == 0)) return true;

         if (left == null || right == null) return false;

         return left.SequenceEqual(right);
      }

      private static string FormatArray(Array? array)
      {
         if (array == null || array.Length == 0) return "null";

         return $"[{string.Join(", ", array.Cast<object>())}]";
      }

      private static string FormatValue(object? value)
      {
         if (value == null) return "null";
         if (value is string[] stringArray) return FormatArray(stringArray);
         if (value is Array array) return FormatArray(array);

         return value.ToString() ?? "null";
      }

      private static IEnumerable<PropertyInfo> Properties()
      {
         return typeof(MediaContainer)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => Attribute.IsDefined(p, typeof(SolrFieldAttribute)) ||
                        Attribute.IsDefined(p, typeof(SolrUniqueKeyAttribute)));
      }

      public IEnumerable<string> Differences(MediaContainer other)
      {
         var properties = Properties();
         var differences = new List<string>();

         foreach (var property in properties)
         {
            var left = property.GetValue(this);
            var right = property.GetValue(other);

            if (!Compare(left, right))
            {
               differences.Add($"{property.Name}: '{FormatValue(left)}' -> '{FormatValue(right)}'");
            }
         }

         return differences;
      }

      public static bool operator ==(MediaContainer? left, MediaContainer? right) => Equals(left, right);

      public static bool operator !=(MediaContainer? left, MediaContainer? right) => !Equals(left, right);
   }
}
