using System;
using SolrNet.Attributes;

namespace MediaCurator.Models
{
   public class MediaContainer
   {
      #region MediaContainer

      [SolrUniqueKey("id")]
      public string Id { get; set; }

      [SolrField("name")]
      public string Name { get; set; }

      [SolrField("description")]
      public string Description { get; set; }

      [SolrField("type")]
      public string Type { get; set; }

      [SolrField("path")]
      public string Path { get; set; }

      [SolrField("fullPath")]
      public string FullPath { get; set; }

      [SolrField("flags")]
      public string[] Flags { get; set; }

      #endregion // MediaContainer

      #region MediaFile

      [SolrField("size")]
      public long Size { get; set; }

      [SolrField("dateCreated")]
      public DateTime DateCreated { get; set; }

      [SolrField("dateModified")]
      public DateTime DateModified { get; set; }

      [SolrField("thumbnails")]
      public int Thumbnails { get; set; }

      #endregion // MediaFile

      #region VideoFile

      [SolrField("duration")]
      public double Duration { get; set; }

      [SolrField("width")]
      public uint Width { get; set; }

      [SolrField("height")]
      public uint Height { get; set; }

      #endregion // VideoFile
   }
}
