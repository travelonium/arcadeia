using System;
using System.IO;
using System.Xml.Linq;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Generic;
using System.Threading;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ImageMagick;

namespace MediaCurator
{
   class PhotoFile : MediaFile
   {
      #region Constants

      #endregion // Constants

      #region Fields

      public ResolutionType Resolution
      {
         get
         {
            return new ResolutionType(Tools.GetAttributeValue(Self, "Resolution"));
         }

         set
         {
            Tools.SetAttributeValue(Self, "Resolution", value.ToString());
         }
      }

      /// <summary>
      /// Gets a tailored MediaContainer model describing a photo file.
      /// </summary>
      public override Models.MediaContainer Model
      {
         get
         {
            var model = base.Model;

            model.Width = Resolution.Width;
            model.Height = Resolution.Height;

            return model;
         }

         set
         {
            base.Model = value;
         }
      }

      #endregion // Fields

      #region Constructors

      public PhotoFile(IConfiguration configuration, IThumbnailsDatabase thumbnailsDatabase, IMediaLibrary mediaLibrary, string path)
         : base(configuration, thumbnailsDatabase, mediaLibrary, "Photo", path)
      {
         // The base class constructor will take care of the parents and the creation or retrieval
         // of the element itself. Here we'll attend to additional properties of a photo file.

         if (Self != null)
         {
            if (Created)
            {
               // This element did not exist before and has been createdn or it did exist but it has
               // been marked as "Modified". Therefore, the missing/updated fields need be filled in here.
            }
         }
      }

      public PhotoFile(IConfiguration configuration, IThumbnailsDatabase thumbnailsDatabase, IMediaLibrary mediaLibrary, XElement element, bool update = false)
         : base(configuration, thumbnailsDatabase, mediaLibrary, element, update)
      {
         if (Self != null)
         {
            if (update)
            {
               if (Modified)
               {
                  // This file existed before but has changed since the last scan.
               }
            }
         }
      }

      #endregion // Constructors

      #region Photo File Operations

      public MagickImageInfo GetImageInfo(string path = null)
      {
         if (path == null)
         {
            path = FullPath;
         }

         try
         {
            return new MagickImageInfo(path);
         }
         catch (Exception e)
         {
            Debug.WriteLine(FullPath + " : ");
            Debug.WriteLine(e.Message);
         }

         return null;
      }

      public override void GetFileInfo(string path)
      {
         var info = GetImageInfo(path);

         if (info != null)
         {
            /*--------------------------------------------------------------------------------
                                                RESOLUTION
            --------------------------------------------------------------------------------*/

            Resolution = new ResolutionType(String.Format("{0}x{1}", info.Width, info.Height));
         }
      }

      public byte[] Preview(int width = 0, int height = 0, MagickFormat format = MagickFormat.Jpeg)
      {
         byte[] output = null;

         using (var image = new MagickImage(FullPath))
         {
            try
            {
               // Convert the photo's format in case the encoder is missing as it is in case of HEIC files
               image.Format = format;

               image.AutoOrient();

               if ((width > 0) || (height > 0))
               {
                  var size = new MagickGeometry(width, height)
                  {
                     IgnoreAspectRatio = false,
                     Greater = true,
                     Less = false,
                  };

                  image.Resize(size);
               }

               output = image.ToByteArray();
            }
            catch (Exception e)
            {
               Debug.WriteLine("Failed To Resize: " + FullPath);
               Debug.WriteLine(e.Message);
            }
         }

         return output;
      }

      private byte[] GenerateThumbnail(string path)
      {
         int width = 720;
         int height = width / 16 * 9;
         byte[] output = null;

         using (var image = new MagickImage(path))
         {
            var size = new MagickGeometry(width, height)
            {
               IgnoreAspectRatio = false,
               FillArea = true
            };

            try
            {
               // Convert the photo's format in case the encoder is missing as it is in case of HEIC files
               image.Format = MagickFormat.Jpeg;

               image.AutoOrient();
               image.Thumbnail(size);
               image.Extent(size, Gravity.Center);
               output = image.ToByteArray();

               if (output.Length > 0)
               {
                  Debug.Write(".");
               }
               else
               {
                  Debug.Write("o");
               }
            }
            catch (Exception)
            {
               Debug.Write("x");
            }
         }

         return output;
      }

      /// <summary>
      /// Overrides the GenerateThumbnails() method of the MediaFile generating thumbnails for the
      /// photo file.
      /// </summary>
      /// <returns>The count of successfully generated thumbnails.</returns>
      public override int GenerateThumbnails()
      {
         int index = 0;
         int total = 1;

         // Make sure the photo file is valid and not corrupted or empty.
         if ((Size == 0) || (Resolution.Height == 0) || (Resolution.Width == 0))
         {
            return index;
         }

         /*----------------------------------------------------------------------------------
                                         GENERATE THUMBNAILS
         ----------------------------------------------------------------------------------*/
         
         Debug.Write("GENERATING THUMBNAILS: " + FullPath);

         Debug.Write(" [");

         for (int counter = 1; counter <= total; counter++)
         {
            // Generate the thumbnail.
            byte[] thumbnail = GenerateThumbnail(FullPath);

            if ((thumbnail != null) && (thumbnail.Length > 0))
            {
               // Add the newly generated thumbnail to the database.
               Thumbnails[index] = thumbnail;

               // Increase the index
               index++;
            }
         }

         Debug.WriteLine("]");

         return index;
      }

      #endregion // Photo File Operations
   }
}