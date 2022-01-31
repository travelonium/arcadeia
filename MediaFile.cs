using System;
using System.IO;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic.FileIO;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;

namespace MediaCurator
{
   public class MediaFile : MediaContainer
   {
      #region Fields

      private long _size = 0;

      /// <summary>
      /// Gets or sets the size of the file.
      /// </summary>
      /// <value>
      /// The file size in UInt64.
      /// </value>
      public long Size
      {
         get => _size;

         set
         {
            if (_size != value)
            {
               _size = value;

               Modified = true;
            }
         }
      }

      /// <summary>
      /// Gets or sets the thumbnail(s) of the current media file.
      /// </summary>
      /// <value>
      /// The thumbnail(s).
      /// </value>
      public MediaFileThumbnails Thumbnails
      {
         get;
         set;
      }

      private string _dateCreated = null;

      /// <summary>
      /// Gets or sets the creation date of the file.
      /// </summary>
      /// <value>
      /// The creation date in DateTime.
      /// </value>
      public DateTime DateCreated
      {
         get => Convert.ToDateTime(_dateCreated, CultureInfo.InvariantCulture);

         set
         {
            if (DateCreated != value)
            {
               _dateCreated = value.ToString(CultureInfo.InvariantCulture);

               Modified = true;
            }
         }
      }

      private string _dateModified = null;

      /// <summary>
      /// Gets or sets the last modification date of the file.
      /// </summary>
      /// <value>
      /// The last modification date in DateTime.
      /// </value>
      public DateTime DateModified
      {
         get => Convert.ToDateTime(_dateModified, CultureInfo.InvariantCulture);

         set
         {
            if (DateModified != value)
            {
               _dateModified = value.ToString(CultureInfo.InvariantCulture);

               Modified = true;
            }
         }
      }

      /// <summary>
      /// Gets the content type (MIME type) of the file.
      /// </summary>
      public string ContentType
      {
         get
         {
            new FileExtensionContentTypeProvider().TryGetContentType(FullPath, out string contentType);

            return contentType;
         }
      }

      /// <summary>
      /// Gets the file extension from its name.
      /// </summary>
      /// <value>
      /// The extension of the file excluding the dot.
      /// </value>
      public string Extension
      {
         get
         {
            var extension = System.IO.Path.GetExtension(Name);

            return (!String.IsNullOrEmpty(extension)) ? extension.ToLower().TrimStart(new Char[] { '.' }) : null;
         }
      }

      /// <summary>
      /// Gets a tailored MediaContainer model describing a media file.
      /// </summary>
      public override Models.MediaContainer Model
      {
         get
         {
            var model = base.Model;

            model.Size = Size;
            model.Thumbnails = Thumbnails.Count;
            model.DateCreated = DateCreated;
            model.DateModified = DateModified;
            model.ContentType = ContentType;
            model.Extension = Extension;

            return model;
         }

         set
         {
            if (value == null) return;

            base.Model = value;

            if (_size == 0) _size = value.Size;
            else
            {
               Size = value.Size;
            }

            Thumbnails = new MediaFileThumbnails(ThumbnailsDatabase, Id);

            if (_dateCreated == null) _dateCreated = value.DateCreated.ToString(CultureInfo.InvariantCulture);
            else
            {
               DateCreated = value.DateCreated;
            }

            if (_dateModified == null) _dateModified = value.DateModified.ToString(CultureInfo.InvariantCulture);
            else
            {
               DateModified = value.DateModified;
            }
         }
      }

      #endregion // Fields

      #region Constructors

      public MediaFile(ILogger<MediaContainer> logger,
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

         Thumbnails = new(ThumbnailsDatabase, Id);

         if (!Exists())
         {
            Deleted = true;

            // Delete the thumbnails belonging to the deleted file.
            Thumbnails.DeleteAll();

            return;
         }

         try
         {
            // Acquire the common file information.
            FileInfo fileInfo = new(path);

            Size = fileInfo.Length;
            DateCreated = fileInfo.CreationTime;
            DateModified = fileInfo.LastWriteTime;
         }
         catch (Exception e)
         {
            // Apparently something went wrong and most details of the file are  irretrievable. Most
            // likely the problem is that the combination of the file name and/or path are too long.
            // Better skip this file altogether.

            Logger.LogWarning("Faile To Retrieve File Information For: {}, Because: {}", path, e.Message);

            Skipped = true;

            return;
         }

         if (Created)
         {
            // Retrieve the media specific file information.
            GetFileInfo(path);
         }

         if (Thumbnails.Initialized)
         {
            if (Created || Modified)
            {
               // Try to regenerate thumbnails for the file.
               GenerateThumbnails();
            }
         }
         else
         {
            // Initialize the record for the file so we wouldn't end up here next time.
            Thumbnails.Initialize();

            // Try to generate thumbnails for the file.
            GenerateThumbnails();
         }
      }

      #endregion // Constructors

      #region Common Functionality

      /// <summary>
      /// Retrieve the media file information and fill in the aquired details. This method is to be
      /// overridden for each individual type of media file with an implementation specific to that type.
      /// </summary>
      /// <param name="path">The full path to the physical file.</param>
      public virtual void GetFileInfo(string path)
      {
         throw new NotImplementedException("This MediaFile does not offer a GetFileInfo() method!");
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
      public virtual int GenerateThumbnails()
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
         if (FullPath.Length > 0)
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

                  Thumbnails.DeleteAll();

                  return;
               }
               catch (Exception e)
               {
                  Logger.LogWarning("Failed To Delete: {}, Because: {}", FullPath, e.Message);
               }
            }
         }
      }

      #endregion // Overrides
   }
}