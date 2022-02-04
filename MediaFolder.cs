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

         if (!Exists())
         {
            Deleted = true;

            return;
         }

         try
         {
            // Acquire the common directory information.
            DirectoryInfo directoryInfo = new(FullPath);

            DateCreated = directoryInfo.CreationTimeUtc;
            DateModified = directoryInfo.LastWriteTimeUtc;
         }
         catch (Exception e)
         {
            // Apparently something went wrong and most details of the directory are irretrievable.
            // Most likely the problem is that the combination of the file name and/or path are too
            // long. Better skip this directory altogether.

            Logger.LogWarning("Failed To Retrieve Directory Information For: {}, Because: {}", FullPath, e.Message);
            Logger.LogDebug("{}", e.ToString());

            Skipped = true;

            return;
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