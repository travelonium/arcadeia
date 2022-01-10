﻿using System;
using System.IO;
using System.Xml.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.VisualBasic.FileIO;
using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.StaticFiles;

namespace MediaCurator
{
   public class MediaFile : MediaContainer
   {
      #region Fields

      /// <summary>
      /// A flag indicating that the MediaFile based element did not exist and has just been
      /// created. This is meant to communicate this to the child constructor which needs to be made
      /// aware and fill in the missing fields.
      /// </summary>
      public bool Created = false;

      /// <summary>
      /// A flag indicating that the file this MediaFile object is associated with has been updated
      /// since the last time the MediaLibrary was scanned. 
      /// </summary>
      public bool Modified = false;

      /// <summary>
      /// Gets or sets the size of the file. This is directly read and written from and to the 
      /// MediaLibrary.
      /// </summary>
      /// <value>
      /// The file size in UInt64.
      /// </value>
      public long Size
      {
         get
         {
            string size = Tools.GetAttributeValue(Self, "Size");
            return Convert.ToInt64((size.Length > 0) ? size : "0");
         }

         set
         {
            Tools.SetAttributeValue(Self, "Size", value.ToString());
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

      /// <summary>
      /// Gets or sets the creation date of the file. This is directly read and written from and to
      /// the MediaLibrary.
      /// </summary>
      /// <value>
      /// The creation date in DateTime.
      /// </value>
      public DateTime DateCreated
      {
         get
         {
            return Convert.ToDateTime(Tools.GetAttributeValue(Self, "DateCreated"), CultureInfo.InvariantCulture);
         }

         set
         {
            Tools.SetAttributeValue(Self, "DateCreated", value.ToString(CultureInfo.InvariantCulture));
         }
      }

      /// <summary>
      /// Gets or sets the last modification date of the file. This is directly read and written 
      /// from and to the MediaLibrary.
      /// </summary>
      /// <value>
      /// The last modification date in DateTime.
      /// </value>
      public DateTime DateModified
      {
         get
         {
            return Convert.ToDateTime(Tools.GetAttributeValue(Self, "DateModified"), CultureInfo.InvariantCulture);
         }

         set
         {
            Tools.SetAttributeValue(Self, "DateModified", value.ToString(CultureInfo.InvariantCulture));
         }
      }

      /// <summary>
      /// Gets or sets the content type (MIME type) of the file. This is directly read and written 
      /// from and to the MediaLibrary.
      /// </summary>
      public string ContentType
      {
         get
         {
            return Tools.GetAttributeValue(Self, "ContentType");
         }

         set
         {
            Tools.SetAttributeValue(Self, "ContentType", value);
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

            return model;
         }

         set
         {
            base.Model = value;
         }
      }

      #endregion // Fields

      #region Constructors

      public MediaFile(IConfiguration configuration, IThumbnailsDatabase thumbnailsDatabase, IMediaLibrary mediaLibrary, string type, string path)
         : base(configuration, thumbnailsDatabase, mediaLibrary, MediaContainer.GetPathComponents(path).Item1)
      {
         // The base class constructor will take care of the parents and below we'll take care of
         // the element itself.

         FileInfo fileInfo = null;

         try
         {
            // Acquire common file information.
            fileInfo = new FileInfo(path);
         }
         catch (Exception e)
         {
            Debug.WriteLine(path + " :");
            Debug.WriteLine(e.Message);

            // Apparently something went wrong and most details of the file are 
            // irretrievable. Most likely the problem is that the combination of the file
            // name and/or path are too long. Better skip this file altogether.

            return;
         }

         if (Parent != null)
         {
            // Generate a unique Id which can be used to create a unique link to each element and 
            // also in creation of a unique folder for each file containing its thumbnails.
            string id = System.IO.Path.GetRandomFileName();

            // Extract the File Name from the supplied path. 
            string name = MediaContainer.GetPathComponents(path).Item2;

            // Retrieve the element if it already exists.
            Self = Tools.GetElementByNameAttribute(Parent.Self, type, name);

            // Make sure we didn't find an already existing element.
            if (Self == null)
            {
               // Looks like there is no such element! Let's create one then. But first gather the 
               // required information common to all file types.

               try
               {
                  Parent.Self.Add(
                     new XElement(type,
                        new XAttribute("Id", id),
                        new XAttribute("Name", name),
                        new XAttribute("Size", fileInfo.Length),
                        new XAttribute("DateCreated", fileInfo.CreationTime.ToString(CultureInfo.InvariantCulture)),
                        new XAttribute("DateModified", fileInfo.LastWriteTime.ToString(CultureInfo.InvariantCulture))
                     )
                  );

                  // Mark this MediaFile based object as one that has just been created. The child 
                  // class will take care of filling in the required missing fields.
                  Created = true;

                  Debug.WriteLine("ADDED : " + path);
               }
               catch (Exception e)
               {
                  throw new Exception("Failed to insert the new " + type + " element due to " +
                                       e.Message);
               }

               // Retrieve the newly created element.
               Self = Tools.GetElementByNameAttribute(Parent.Self, type, name);

               // Make sure that we succeeded to put our hands on it.
               if (Self == null)
               {
                  throw new Exception("Failed to add the new " + type +
                                       " element to the MediaLibrary.");
               }
            }

            // Initialize the flags.
            Flags = new MediaContainerFlags(Self);

            // Initialize the thumbnails.
            Thumbnails = new MediaFileThumbnails(_thumbnailsDatabase, Id);

            // Retrieve and initialize the file's content type if needed, either when a new element
            // has been created or if this is a Startup Scan and the element lacks the attribute.
            if (ContentType == "")
            {
               string contentType = GetContentType();

               if (contentType != null)
               {
                  ContentType = contentType;
               }
            }

            if (Created)
            {
               // Retrieve the media specific file information.
               GetFileInfo(path);

               // Generate the thumbnails for the newly created file.
               GenerateThumbnails();
            }
         }
      }

      public MediaFile(IConfiguration configuration, IThumbnailsDatabase thumbnailsDatabase, IMediaLibrary mediaLibrary, XElement element, bool update = false)
         : base(configuration, thumbnailsDatabase, mediaLibrary, element, update)
      {
         // Initialize the thumbnails.
         Thumbnails = new MediaFileThumbnails(_thumbnailsDatabase, Id);

         // Do we need to update the MediaContainer fields against its physical instance?
         if (update)
         {
            // Verify whether the media file still exists.
            if (Exists())
            {
               if (Flags.Deleted)
               {
                  // Seems like a deleted file has come back to life. Un-Mark the Deleted flag.
                  Flags.Deleted = false;

                  // Check if there are any thumbnails already generated for this file or if the file
                  // is modified in which case we need to regenerate the thumbnails.
                  if ((Thumbnails.Count == 0) || (Modified))
                  {
                     // Regenerate the thumbnails for the deleted file if necessary.
                     GenerateThumbnails();
                  }

                  Debug.WriteLine("RESTORED : " + FullPath);
               }

               try
               {
                  // Acquire common file information.
                  FileInfo fileInfo = new FileInfo(FullPath);

                  if ((Size != fileInfo.Length) ||
                      (DateCreated.ToString(CultureInfo.InvariantCulture) != fileInfo.CreationTime.ToString(CultureInfo.InvariantCulture)) ||
                      (DateModified.ToString(CultureInfo.InvariantCulture) != fileInfo.LastWriteTime.ToString(CultureInfo.InvariantCulture)))
                  {
                     // Mark the file as Modified so that the child MediaFile would take care of it.
                     Modified = true;

                     // Now update the common properties with what we read from the file itself.
                     Size = fileInfo.Length;
                     DateCreated = fileInfo.CreationTime;
                     DateModified = fileInfo.LastWriteTime;

                     // Retrieve and initialize the file's content type if needed
                     string contentType = GetContentType();
                     if (contentType != null)
                     {
                        ContentType = contentType;
                     }

                     // Remove the previously generated thumbnails as they are most likely out of date.
                     Thumbnails.DeleteAll();

                     // Retrieve the media specific file information.
                     GetFileInfo(FullPath);

                     // Regenerate the thumbnails for the modified file.
                     GenerateThumbnails();

                     Debug.WriteLine("UPDATED : " + FullPath);
                  }
               }
               catch (Exception e)
               {
                  Debug.WriteLine(FullPath + " : ");
                  Debug.WriteLine(e.Message);

                  // Apparently something went wrong and most details of the file are 
                  // irretrievable. Most likely the problem is that the combination of the file
                  // name and/or path are too long. Nevermind.
               }
            }
            else
            {
               if (!Flags.Deleted)
               {
                  // No, it does not. Mark it as deleted.
                  Flags.Deleted = true;

                  // Mark the file as Modified so that the child MediaFile would take care of it.
                  Modified = true;

                  // Delete the thumbnails belonging to the deleted file.
                  Thumbnails.DeleteAll();

                  Debug.WriteLine("DELETED : " + FullPath);
               }
            }
         }
      }

      #endregion // Constructors

      #region Common Functionality

      /// <summary>
      /// Retrieves the file's content (MIME) type.
      /// </summary>
      /// <returns>The MIME type as a string or null in case it couldn't be determined.</returns>
      public string GetContentType()
      {
         new FileExtensionContentTypeProvider().TryGetContentType(FullPath, out string contentType);

         return contentType;
      }

      /// <summary>
      /// Removes the associated element from the MediaLibrary. This is if for example at a later
      /// point we regret having created this element.
      /// </summary>
      public void Remove()
      {
         if (Self != null)
         {
            // Remove the element from the MediaLibrary.
            Self.Remove();

            // Set the element references to null.
            Self = null;
            Parent = null;
         }
      }

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
      /// <param name="deleteXmlEntry">if set to <c>true</c> the node itself in the MediaLibrary is also
      /// deleted.</param>
      /// <remarks>
      /// There still exists a specific case which is not handled correctly. If the file to be deleted
      /// is in use by another process or program, Windows will prompt the user so and he can choose
      /// whether they want to try again or cancel the operation. The intention here was that it would
      /// simply queue the delete without notifying the user and attempt to delete the file cyclically
      /// until it succeeds. However, since the <seealso cref="FileSystem.DeleteFile"/> if used with 
      /// the <seealso cref="RecycleOption"/> does not throw the <seealso cref="System.IO.IOException"/>,
      /// it currently will give up if the user chooses to cancel the delete. 
      /// </remarks>
      public override void Delete(bool permanent = false, bool deleteXmlEntry = false)
      {
         if (FullPath.Length > 0)
         {
            if (Exists())
            {
               Task.Run(async delegate
               {
                  while (true)
                  {
                     Debug.Write("Deleting " + FullPath + "...");

                     try
                     {
                        FileSystem.DeleteFile(FullPath,
                                             UIOption.OnlyErrorDialogs,
                                             permanent ? RecycleOption.DeletePermanently :
                                                         RecycleOption.SendToRecycleBin,
                                             UICancelOption.ThrowException);

                        if (deleteXmlEntry)
                        {
                           // Remove the MediaLibrary entry itself.
                           this.Remove();
                        }
                        else
                        {
                           // Only set the Deleted flag on the MediaLibrary entry.
                           Flags.Deleted = true;
                        }

                        if (Thumbnails.DeleteAll() > 0)
                        {
                           // Delete the thumbnails now.
                           Debug.WriteLine("done!");
                        }

                        return;
                     }
                     catch (System.IO.IOException)
                     {
                        // The file appears to be in-use. Try again later...
                        Debug.WriteLine("file in use! Will try again later...");
                     }
                     catch (System.OperationCanceledException)
                     {
                        // The user has canceled the delete operation.
                        return;
                     }
                     catch (Exception)
                     {
                        throw;
                     }

                     await Task.Delay(10000);
                  }
               });
            }
         }
      }

      #endregion // Overrides
   }
}