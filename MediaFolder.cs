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

      /// <summary>
      /// Moves (or Renames) the MediaFolder from one location or name to another.
      /// </summary>
      /// <param name="source">The fullpath of the original name and location.</param>
      /// <param name="destination">The fullpath of the new name and location.</param>
      public override void Move(string source, string destination)
      {
         // TODO: Moving folders need a bit more work:
         //       - Check whether the destination files and folders already exist on disk or in the
         //         library and throw one or severals exceptions if so.
         //       - All the child MediaContainers and their children need to move to the new location.
         //       - Focus solely on the MediaContainers and leave the other files be as it would get
         //         so much more complicated otherwise.
         //       - When moving, remove the moved folder if it is empty of other files.
         //
         // Directory.Move(source, destination);

         base.Move(source, destination);
      }

      #endregion
   }
}