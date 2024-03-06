using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using MediaCurator.Services;

namespace MediaCurator
{
   public class MediaFolder : MediaContainer
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

         var fileSystemService = Services.GetService<IFileSystemService>();

         if (!Exists())
         {
            // Avoid updating or removing the folder if it was located in a network mount that is currently unavailable.
            if (fileSystemService.Mounts.Any(mount => (FullPath.StartsWith(mount.Folder) && !mount.Available)))
            {
               Skipped = true;
            }
            else
            {
               Deleted = true;
            }

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
      /// <param name="destination">The fullpath of the new name and location.</param>
      public override void Move(string destination)
      {
         // TODO: Moving folders need a bit more work:
         //       - Check whether the destination files and folders already exist on disk or in the
         //         library and throw one or severals exceptions if so.
         //       - All the child MediaContainers and their children need to move to the new location.
         //       - Focus solely on the MediaContainers and leave the other files be as it would get
         //         so much more complicated otherwise.
         //       - When moving, remove the moved folder if it is empty of other files.

         // Split the path in parent, child components.
         var pathComponents = GetPathComponents(destination);

         Directory.CreateDirectory(destination);

         // Update the Name if necessary.
         Name = pathComponents.Child;

         foreach (var child in Children)
         {
            child.Move(System.IO.Path.Combine(destination, child.Name));
            child.Save();
         }

         if (!Children.Any())
         {
            try
            {
               // Attempt to delete the folder in its old location if it is empty now.
               Directory.Delete(FullPath);
            }
            catch (IOException e)
            {
               Logger.LogDebug("Folder Not Deleted: {}, Beacuse: {}", FullPath, e.ToString());
            };

            // Flag the old MediaFolder as Deleted to be removed from the index since it lacks any children.
            Deleted = true;

            /*
            // Save the old MediaFolder before replacing it with its new model.
            Save();

            // Reload the MediaFolder so the returned response is current.
            var path = destination.EndsWith(Platform.Separator.Path) ? destination : destination + Platform.Separator.Path;
            var model = Load(id: null, path: path);

            if (model != null)
            {
               Moved = true;
               Deleted = false;

               Model = model;
            }
            */
         }
         else
         {
            Logger.LogError("Folder Has Children After Move: {}", FullPath);
         }
      }

      #endregion
   }
}