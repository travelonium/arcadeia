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

using SolrNet;
using Arcadeia.Solr;
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
            // Avoid updating or removing the folder if it was located in a network mount that is currently unavailable
            // or if it's in the middle of being moved.
            if (fileSystemService.Mounts.Any(mount => FullPath != null && FullPath.StartsWith(mount.Folder) && !mount.Attached) || MediaLibrary.IsLocked(FullPath))
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
      /// Renames the MediaFolder by moving the physical folder as a whole and then updating the
      /// paths of all the MediaContainers within it in the index, keeping their ids so their
      /// attributes and thumbnails are preserved.
      /// </summary>
      /// <param name="destination">The full path of the new name within the same parent.</param>
      public override void Move(string destination)
      {
         if (string.IsNullOrEmpty(Id)) throw new ArgumentNullException(nameof(Id), "The Id cannot be null or empty.");

         if (string.IsNullOrEmpty(FullPath)) throw new ArgumentNullException(nameof(FullPath), "The FullPath cannot be null or empty.");

         var source = FullPath;
         var target = EnsureTrailingSlash(destination.TrimEnd('/', '\\'))!;
         var pathComponents = GetPathComponents(target);
         var name = pathComponents.Child?.Trim('/', '\\');

         if (string.IsNullOrEmpty(name)) throw new ArgumentException("The destination folder name cannot be empty.", nameof(destination));

         if (pathComponents.Parent != Path) throw new NotSupportedException("Folders can only be renamed and not moved.");

         if (target == source) return;

         // Allow case-only renames on case-insensitive file systems where the destination would appear to exist already.
         if ((Directory.Exists(target) || File.Exists(target.TrimEnd('/', '\\'))) && !string.Equals(target, source, StringComparison.OrdinalIgnoreCase))
         {
            throw new IOException("A file or folder with the same name already exists.");
         }

         // Keep anything else, e.g. the scanner, off both locations until the index has caught up.
         using var _ = MediaLibrary.LockPaths(source, target);

         var previousName = Name;

         Directory.Move(source, target);

         try
         {
            using IServiceScope scope = Services.CreateScope();
            ISolrIndexService<Models.MediaContainer> solrIndexService = scope.ServiceProvider.GetRequiredService<ISolrIndexService<Models.MediaContainer>>();

            // Rewrite the paths of all the descendants directly in the index rather than loading
            // each of them, as that would mark them deleted while their parents still point to the
            // old location.
            var documents = solrIndexService.Get(new SolrQuery("path:" + EscapeQueryValue(source) + "*"))
                                            .Where(descendant => descendant.Path?.StartsWith(source, StringComparison.Ordinal) ?? false)
                                            .ToList();

            foreach (var descendant in documents)
            {
               descendant.Path = target + descendant.Path![source.Length..];
               descendant.FullPath = (descendant.FullPath?.StartsWith(source, StringComparison.Ordinal) ?? false) ? target + descendant.FullPath[source.Length..] : descendant.FullPath;
            }

            // Update the folder itself in the same batch so the index is never left half renamed.
            Name = name;

            var model = Model;

            documents.Add(model);

            if (!solrIndexService.Update(documents))
            {
               throw new InvalidOperationException("Failed to update the folder and its contents in the index.");
            }

            Original = model;

            Logger.LogInformation("Folder Renamed: {} -> {}, Updated {} Descendants", source, target, documents.Count - 1);
         }
         catch
         {
            Name = previousName;

            // Put the folder back where it was so the disk and the index remain consistent.
            try
            {
               Directory.Move(target, source);
            }
            catch (Exception e)
            {
               Logger.LogError("Failed To Restore Folder: {} -> {}, Because: {}", target, source, e.Message);
            }

            throw;
         }

         // Forget the ids generated for the old paths so new containers created there won't reuse them.
         MediaLibrary.ClearCache(source);

         Moved = true;
      }

      #endregion

      #region Private Methods

      private static string EscapeQueryValue(string value)
      {
         var result = new System.Text.StringBuilder();

         foreach (var character in value)
         {
            if ("+-&|!(){}[]^\"~*?:\\/ ".Contains(character)) result.Append('\\');
            result.Append(character);
         }

         return result.ToString();
      }

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