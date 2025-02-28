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

using System.IO.Hashing;
using System.Globalization;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic.FileIO;
using Microsoft.AspNetCore.StaticFiles;
using Arcadeia.Configuration;
using Arcadeia.Services;

namespace Arcadeia
{
   public class MediaFile : MediaContainer
   {
      #region Fields

      /// <summary>
      /// Gets or sets the size of the file.
      /// </summary>
      /// <value>
      /// The file size in Int64.
      /// </value>
      public long Size { get; set; }

      /// <summary>
      /// Gets or sets the thumbnail(s) of the current media file.
      /// </summary>
      /// <value>
      /// The thumbnail(s).
      /// </value>
      public MediaFileThumbnails? Thumbnails { get; set; }

      /// <summary>
      /// Gets the content type (MIME type) of the file.
      /// </summary>
      public string? ContentType
      {
         get
         {
            if (string.IsNullOrEmpty(FullPath)) return null;

            new FileExtensionContentTypeProvider().TryGetContentType(FullPath, out string? contentType);

            return contentType;
         }
      }

      /// <summary>
      /// Gets the file extension from its name.
      /// </summary>
      /// <value>
      /// The extension of the file excluding the dot.
      /// </value>
      public string? Extension
      {
         get
         {
            var extension = System.IO.Path.GetExtension(Name);

            return (!string.IsNullOrEmpty(extension)) ? extension.ToLower().TrimStart(['.']) : null;
         }
      }

      /// <summary>
      /// Gets or sets the views count of the file.
      /// </summary>
      /// <value>
      /// The views count in Int64.
      /// </value>
      public long Views { get; set; }

      /// <summary>
      /// Gets or sets the checksum of the file.
      /// </summary>
      /// <value>
      /// The checksum in String.
      /// </value>
      public string? Checksum { get; set; }

      /// <summary>
      /// Gets or sets the date the file was accessed last.
      /// </summary>
      /// <value>
      /// The last access date in DateTime.
      /// </value>
      public DateTime? DateAccessed { get; set; }

      /// <summary>
      /// Gets a tailored MediaContainer model describing a media file.
      /// </summary>
      public override Models.MediaContainer Model
      {
         get
         {
            var model = base.Model;

            model.Size = Size;
            model.Thumbnails = Thumbnails?.Count ?? 0;
            model.ContentType = ContentType;
            model.Extension = Extension;
            model.Views = Views;
            model.Checksum = Checksum;
            model.DateAccessed = DateAccessed;

            return model;
         }

         set
         {
            if (value == null) return;

            base.Model = value;

            Size = value.Size ?? 0;
            Checksum = value.Checksum;
            Views = value.Views ?? 0;
            DateAccessed = value.DateAccessed;

            if (string.IsNullOrEmpty(Id)) throw new ArgumentNullException(nameof(Id), "The Id cannot be null or empty.");

            Thumbnails = new(ThumbnailsDatabase, Id);
         }
      }

      #endregion // Fields

      #region Constructors

      public MediaFile(ILogger<MediaContainer> logger,
                       IServiceProvider services,
                       IOptionsMonitor<Settings> settings,
                       IThumbnailsDatabase thumbnailsDatabase,
                       IMediaLibrary mediaLibrary,
                       string? id = null, string? path = null,
                       IProgress<float>? progress = null
      ) : base(logger, services, settings, thumbnailsDatabase, mediaLibrary, id, path, progress)
      {
         // The base class constructor will take care of the entry, its general attributes and its
         // parents and below we'll take care of its specific attributes.

         if (Skipped) return;

         if (string.IsNullOrEmpty(Id)) throw new ArgumentNullException(nameof(Id), "The Id cannot be null or empty.");

         if (string.IsNullOrEmpty(FullPath)) throw new ArgumentNullException(nameof(FullPath), "The FullPath cannot be null or empty.");

         Thumbnails = new(ThumbnailsDatabase, Id);

         var fileSystemService = Services.GetRequiredService<IFileSystemService>();

         if (!Exists())
         {
            // Avoid updating or removing the file if it was located in a network mount that is currently unavailable.
            if (fileSystemService.Mounts.Any(mount => FullPath != null && FullPath.StartsWith(mount.Folder) && !mount.Attached))
            {
               Skipped = true;
            }
            else
            {
               Deleted = true;

               // Delete the thumbnails belonging to the deleted file.
               Thumbnails.DeleteAll();
            }

            return;
         }

         try
         {
            // Acquire the common file information.
            FileInfo fileInfo = new(FullPath);

            Size = fileInfo.Length;
            DateCreated = fileInfo.CreationTimeUtc != DateTime.UnixEpoch ? fileInfo.CreationTimeUtc : null;
            DateModified = fileInfo.LastWriteTimeUtc != DateTime.UnixEpoch ? fileInfo.LastWriteTimeUtc : null;
         }
         catch (Exception e)
         {
            // Apparently something went wrong and most details of the file are  irretrievable. Most
            // likely the problem is that the combination of the file name and/or path are too long.
            // Better skip this file altogether.

            Logger.LogWarning("Failed To Retrieve File Information For: {}, Because: {}", FullPath, e.Message);

            Skipped = true;

            return;
         }

         if
         (
            (Created || Modified) &&
            (
               (Size != Original?.Size) ||
               (DateModified.TruncateToSeconds() != Original?.DateModified.TruncateToSeconds()) ||
               (DateCreated.TruncateToSeconds() != Original?.DateCreated.TruncateToSeconds())
            )
         )
         {
            try
            {
               Checksum = Size > 0 ? GetFileChecksum(FullPath) : null;
            }
            catch (Exception e)
            {
               Logger.LogWarning("Failed To Calculate File Checksum For: {}, Because: {}", FullPath, e.Message);
            }
         }

         if ((Created || Modified) && (Checksum != Original?.Checksum))
         {
            try
            {
               GetFileInfo(FullPath, Size);
            }
            catch (Exception e)
            {
               Logger.LogWarning("Failed To Retrieve MediaFile Information For: {}, Because: {}", FullPath, e.Message);
            }
         }

         try
         {
            if (Thumbnails.Initialized)
            {
               if ((Created || Modified) && (Checksum != Original?.Checksum))
               {
                  // Try to regenerate thumbnails for the file.
                  GenerateThumbnails(force: true);
               }
               else if (Settings.CurrentValue.Scanner.ForceGenerateMissingThumbnails)
               {
                  // Try to generate the missing thumbnails for the file.
                  GenerateThumbnails();
               }
            }
            else
            {
               // Initialize the record for the file so we wouldn't end up here next time.
               Thumbnails.Initialize();

               // Try to generate thumbnails for the file.
               GenerateThumbnails(force: true);
            }
         }
         catch (Exception e)
         {
            Logger.LogWarning("Failed To Generate Thumbnails For: {}, Because: {}", FullPath, e.Message);
         }
      }

      #endregion // Constructors

      #region Common Functionality

      /// <summary>
      /// Retrieve the media file information and fill in the acquired details. This method is to be
      /// overridden for each individual type of media file with an implementation specific to that type.
      /// </summary>
      /// <param name="path">The full path to the physical file.</param>
      /// <param name="size">The size of the physical file.</param>
      public virtual void GetFileInfo(string path, long size)
      {
         throw new NotImplementedException("This MediaFile does not offer a GetFileInfo() method!");
      }


      /// <summary>
      /// Calculate the media file checksum. This method can be overridden for each individual type of
      // media file with an implementation specific to that type.
      /// </summary>
      /// <param name="path">The full path to the physical file.</param>
      public virtual string GetFileChecksum(string path)
      {
         Logger.LogDebug("Calculating Checksum For: {}", path);

         int bytesRead;
         var hasher = new XxHash3();
         const int bufferSize = 1024 * 1024;
         byte[] buffer = new byte[bufferSize];
         using var stream = new BufferedStream(File.OpenRead(path), bufferSize);

         while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
         {
            hasher.Append(buffer.AsSpan(0, bytesRead));
         }

         return BitConverter.ToString(hasher.GetCurrentHash()).Replace("-", "").ToLower();
      }

      /// <summary>
      /// Generates the thumbnails for the current media file. This method is to be overridden for
      /// each individual type of media file with an implementation specific to that type.
      /// </summary>
      /// <param name="progress">The progress which is reflected in a ProgressBar and indicates how
      /// far the thumbnail generation for the current file has gone.</param>
      /// <param name="preview">The name of the recently generated thumbnail file in order to be
      /// previewed for the user.</param>
      /// <returns>The count of successfully generated thumbnails.</returns>
      public virtual int GenerateThumbnails(bool force = false)
      {
         throw new NotImplementedException("This MediaFile does not offer a GenerateThumbnails() method!");
      }

      #endregion // Common Functionality

      #region Overrides

      /// <summary>
      /// Checks whether or not this media file has physical existence.
      /// </summary>
      /// <returns>
      ///   <c>true</c> if the file physically exists and <c>false</c> otherwise.
      /// </returns>
      public override bool Exists()
      {
         return File.Exists(FullPath);
      }

      /// <summary>
      /// Deletes the MediaFile from the disk and the MediaLibrary. Each MediaFile type can
      /// optionally override and implement its own delete method or its additional delete actions.
      /// </summary>
      /// <param name="permanent">if set to <c>true</c>, it permanently deletes the file from the disk
      /// and otherwise moves it to the recycle bin.</param>
      /// <remarks>
      /// There still exists a specific case which is not handled correctly. If the file to be deleted
      /// is in use by another process or program, Windows will prompt the user so and he can choose
      /// whether they want to try again or cancel the operation. The intention here was that it would
      /// simply queue the delete without notifying the user and attempt to delete the file cyclically
      /// until it succeeds. However, since the <seealso cref="FileSystem.DeleteFile"/> if used with
      /// the <seealso cref="RecycleOption"/> does not throw the <seealso cref="System.IO.IOException"/>,
      /// it currently will give up if the user chooses to cancel the delete.
      /// </remarks>
      public override void Delete(bool permanent = false)
      {
         if (!string.IsNullOrEmpty(FullPath))
         {
            if (Exists())
            {
               try
               {
                  FileSystem.DeleteFile(FullPath, UIOption.OnlyErrorDialogs,
                                        permanent ? RecycleOption.DeletePermanently :
                                                    RecycleOption.SendToRecycleBin,
                                        UICancelOption.ThrowException);

                  Deleted = true;

                  Thumbnails?.DeleteAll();

                  return;
               }
               catch (Exception e)
               {
                  Logger.LogWarning("Failed To Delete: {}, Because: {}", FullPath, e.Message);
               }
            }
         }
      }

      /// <summary>
      /// Moves (or Renames) the MediaFile from one location or name to another.
      /// </summary>
      /// <param name="destination">The full path of the new name and location.</param>
      public override void Move(string destination)
      {
         if (string.IsNullOrEmpty(FullPath)) throw new ArgumentNullException(nameof(FullPath), "The FullPath cannot be null or empty.");

         // Split the path in parent, child components.
         var pathComponents = GetPathComponents(destination);

         try
         {
            if (pathComponents.Parent != null)
            {
               Directory.CreateDirectory(pathComponents.Parent);
            }

            File.Move(FullPath, destination);

            // Update the MediaFile's Name.
            Name = pathComponents.Child;

            // Now that the MediaFile has successfully been moved, update its Parent if needed.
            if (pathComponents.Parent != null)
            {
               // Determine whether the Parent needs to be updated.
               if (pathComponents.Parent != Path)
               {
                  // Yes, we need to update the Parent, let's infer the new Parent's type.
                  Type? parentType = GetMediaContainerType(GetPathComponents(pathComponents.Parent).Child);

                  if (parentType is not null)
                  {
                     // Now let's instantiate the new Parent.
                     Parent = Activator.CreateInstance(parentType, Logger, Services, Settings, ThumbnailsDatabase, MediaLibrary, null, pathComponents.Parent, Progress) as MediaContainer;
                     ParentType = Parent?.Type;
                  }
               }
            }
            else
            {
               if (MediaLibrary.Path != Path)
               {
                  // The path supplied refers to the root of the MediaLibrary i.e. the "/". We simply set
                  // the parent to the MediaLibrary instance itself.

                  Parent = MediaLibrary;
                  ParentType = Parent.Type;
               }
            }
         }
         catch
         {
            throw;
         }
      }

      #endregion // Overrides
   }
}