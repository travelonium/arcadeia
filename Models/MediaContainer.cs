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

using System;
using SolrNet.Attributes;

namespace Arcadeia.Models
{
   public class MediaContainer
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

      [SolrField("description")]
      public string? Description { get; set; }

      [SolrField("path")]
      public string? Path { get; set; }

      [SolrField("fullPath")]
      public string? FullPath { get; set; }

      [SolrField("size")]
      public long Size { get; set; }

      [SolrField("contentType")]
      public string? ContentType { get; set; }

      [SolrField("extension")]
      public string? Extension { get; set; }

      [SolrField("views")]
      public long Views { get; set; }

      [SolrField("dateAccessed")]
      public DateTime? DateAccessed { get; set; }

      [SolrField("dateAdded")]
      public DateTime DateAdded { get; set; }

      [SolrField("dateCreated")]
      public DateTime DateCreated { get; set; }

      [SolrField("dateModified")]
      public DateTime DateModified { get; set; }

      [SolrField("dateTaken")]
      public DateTime? DateTaken { get; set; }

      [SolrField("thumbnails")]
      public int Thumbnails { get; set; }

      [SolrField("duration")]
      public double Duration { get; set; }

      [SolrField("width")]
      public long Width { get; set; }

      [SolrField("height")]
      public long Height { get; set; }

      [SolrField("flags")]
      public string[]? Flags { get; set; }
   }
}
