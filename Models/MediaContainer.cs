using System;

namespace MediaCurator.Models
{
   public class MediaContainer
   {
      #region MediaContainer

      public string Id { get; set; }

      public string Name { get; set; }

      public string Type { get; set; }

      public string FullPath { get; set; }

      public string[] Flags { get; set; }

      #endregion // MediaContainer

      #region MediaFile

      public long Size { get; set; }

      public DateTime DateCreated { get; set; }

      public DateTime DateModified { get; set; }

      public MediaFileThumbnails Thumbnails { get; set; }

      #endregion // MediaFile

      #region VideoFile

      public double Duration { get; set; }

      public ResolutionType Resolution { get; set; }

      #endregion // VideoFile

   }
}
