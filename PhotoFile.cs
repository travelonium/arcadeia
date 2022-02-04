using System;
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using ImageMagick;

namespace MediaCurator
{
   class PhotoFile : MediaFile
   {
      #region Constants

      private Lazy<Dictionary<string, Dictionary<string, int>>> ThumbnailsConfiguration => new(() =>
      {
         var section = Configuration.GetSection("Thumbnails:Photo");

         if (section.Exists())
         {
            return section.Get<Dictionary<string, Dictionary<string, int>>>();
         }

         return new Dictionary<string, Dictionary<string, int>>();
      });

      #endregion // Constants

      #region Fields

      private long _width  = -1;
      private long _height = -1;

      /// <summary>
      /// Gets or sets the resolution of the photo file.
      /// </summary>
      /// <value>
      /// The photo file resolution.
      /// </value>
      public ResolutionType Resolution
      {
         get => new(_width, _height);

         set
         {
            if (Resolution != value)
            {
               Modified = ((_width >= 0) || (_height >= 0));

               _width = value.Width;
               _height = value.Height;
            }
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
            if (value == null) return;

            base.Model = value;

            Resolution = new(value.Width, value.Height);
         }
      }

      #endregion // Fields

      #region Constructors

      public PhotoFile(ILogger<MediaContainer> logger,
                       IServiceProvider services,
                       IConfiguration configuration,
                       IThumbnailsDatabase thumbnailsDatabase,
                       IMediaLibrary mediaLibrary,
                       string id = null, string path = null
      ) : base(logger, services, configuration, thumbnailsDatabase, mediaLibrary, id, path)
      {
         // The base class constructor will take care of the entry, its general attributes and its
         // parents and below we'll take care of its specific attributes.
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

            Resolution = new ResolutionType(info.Width, info.Height);
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

      private byte[] GenerateThumbnail(string path, int width, int height, bool crop)
      {
         byte[] output = null;

         using (var image = new MagickImage(path))
         {
            try
            {
               var size = new MagickGeometry((width > 0) ? width : image.BaseWidth, (height > 0) ? height : image.BaseHeight)
               {
                  IgnoreAspectRatio = false,
                  FillArea = crop
               };

               // Convert the photo's format in case the encoder is missing as it is in case of HEIC files
               image.Format = MagickFormat.Jpeg;

               image.AutoOrient();
               image.Thumbnail(size);

               if (crop)
               {
                  image.Extent(size, Gravity.Center);
               }

               output = image.ToByteArray();

               /*
               if (output.Length > 0)
               {
                  Debug.Write(".");
               }
               else
               {
                  Debug.Write("o");
               }
               */
            }
            catch (Exception)
            {
               // Debug.Write("x");
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
         int total = 0;

         // Make sure the photo file is valid and not corrupted or empty.
         if ((Size == 0) || (Resolution.Height == 0) || (Resolution.Width == 0))
         {
            return total;
         }

         /*----------------------------------------------------------------------------------
                                         GENERATE THUMBNAILS
         ----------------------------------------------------------------------------------*/
         
         // Debug.Write("GENERATING THUMBNAILS: " + FullPath);

         // Debug.Write(" [");

         foreach (var item in ThumbnailsConfiguration.Value)
         {
            int count = 0;
            int width = -1;
            int height = -1;
            bool crop = false;
            string label = item.Key;

            if (item.Value.ContainsKey("Count")) count = item.Value["Count"];
            if (item.Value.ContainsKey("Width")) width = item.Value["Width"];
            if (item.Value.ContainsKey("Height")) height = item.Value["Height"];
            if (item.Value.ContainsKey("Crop")) crop = (item.Value["Crop"] > 0);

            for (int counter = 0; counter < Math.Max(1, count); counter++)
            {
               // Generate the thumbnail.
               byte[] thumbnail = GenerateThumbnail(FullPath, width, height, crop);

               if ((thumbnail != null) && (thumbnail.Length > 0))
               {
                  // Add the newly generated thumbnail to the database.
                  if (count >= 1)
                  {
                     Thumbnails[counter] = thumbnail;
                  }
                  else
                  {
                     Thumbnails[label] = thumbnail;
                  }

                  total++;
               }
            }
         }

         // Debug.WriteLine("]");

         if (total > 0)
         {
            Modified = true;
         }

         return total;
      }

      #endregion // Photo File Operations
   }
}