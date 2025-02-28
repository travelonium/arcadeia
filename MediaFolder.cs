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

using Microsoft.Extensions.Options;
using Arcadeia.Configuration;
using Arcadeia.Services;

namespace Arcadeia
{
   public class MediaFolder : MediaContainer
   {
      #region Fields

      #endregion // Fields

      #region Constructors

      public MediaFolder(ILogger<MediaContainer> logger,
                         IServiceProvider services,
                         IOptionsMonitor<Settings> settings,
                         IThumbnailsDatabase thumbnailsDatabase,
                         IMediaLibrary mediaLibrary,
                         string? id = null, string? path = null,
                         IProgress<float>? progress = null
      ) : base(logger, services, settings, thumbnailsDatabase, mediaLibrary, id, EnsureTrailingSlash(path), progress)
      {
         // The base class constructor will take care of the entry, its general attributes and its
         // parents and below we'll take care of its specific attributes.

         if (Skipped) return;

         var fileSystemService = Services.GetRequiredService<IFileSystemService>();

         if (!Exists())
         {
            // Avoid updating or removing the folder if it was located in a network mount that is currently unavailable.
            if (fileSystemService.Mounts.Any(mount => FullPath != null && FullPath.StartsWith(mount.Folder) && !mount.Attached))
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
            if (string.IsNullOrEmpty(FullPath)) throw new ArgumentNullException(nameof(FullPath), "The FullPath cannot be null or empty.");

            // Acquire the common directory information.
            DirectoryInfo directoryInfo = new(FullPath);

            DateCreated = directoryInfo.CreationTimeUtc != DateTime.UnixEpoch ? directoryInfo.CreationTimeUtc : null;
            DateModified = directoryInfo.LastWriteTimeUtc != DateTime.UnixEpoch ? directoryInfo.LastWriteTimeUtc : null;
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
      /// <param name="destination">The full path of the new name and location.</param>
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
            if (string.IsNullOrEmpty(child.Name)) throw new ArgumentNullException(nameof(child.Name), "The child Name cannot be null or empty.");

            child.Move(System.IO.Path.Combine(destination, child.Name));

            child.Save();
         }

         if (!Children.Any())
         {
            try
            {
               if (string.IsNullOrEmpty(FullPath)) throw new ArgumentNullException(nameof(FullPath), "The FullPath cannot be null or empty.");

               // Attempt to delete the folder in its old location if it is empty now.
               Directory.Delete(FullPath);
            }
            catch (IOException e)
            {
               Logger.LogDebug("Folder Not Deleted: {}, Because: {}", FullPath, e.ToString());
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

      #region Private Methods

      private static string? EnsureTrailingSlash(string? path)
      {
         if (!string.IsNullOrEmpty(path) && !path.EndsWith('/'))
         {
            return path + "/";
         }

         return path;
      }

      #endregion // Private Methods
   }
}