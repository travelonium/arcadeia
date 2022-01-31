using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MediaCurator
{
   class MediaFolder : MediaContainer
   {
      #region Fields

      #endregion // Fields

      #region Constructors


      public MediaFolder(ILogger<MediaContainer> logger,
                         IServiceProvider services,
                         IConfiguration configuration,
                         IThumbnailsDatabase thumbnailsDatabase,
                         IMediaLibrary mediaLibrary,
                         string id = null, string path = null
      ) : base(logger, services, configuration, thumbnailsDatabase, mediaLibrary, id, path)
      {
         // The base class constructor will take care of the entry, its general attributes and its
         // parents and below we'll take care of its specific attributes.

         if (Skipped) return;

         if (Created)
         {
            // Extract the Folder Name from the supplied path removing the \ and / characters.
            Name = MediaContainer.GetPathComponents(path).Child.Trim(new Char[] { '\\', '/' });
         }
      }

      #endregion // Constructors

      #region Overrides

      /// <summary>
      /// Checks whether or not this directory has physical existence.
      /// </summary>
      /// <returns>
      ///   <c>true</c> if the directory physically exists and <c>false</c> otherwise.
      /// </returns>
      public override bool Exists()
      {
         return Directory.Exists(FullPath);
      }

      #endregion
   }
}