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

using System.Globalization;
using Microsoft.Extensions.Options;
using ImageMagick;
using Arcadeia.Configuration;

namespace Arcadeia
{
   class PhotoFile : MediaFile
   {
      #region Constants

      #endregion // Constants

      #region Fields

      /// <summary>
      /// Gets or sets the date the photo was originally taken.
      /// </summary>
      /// <value>
      /// The original date in DateTime.
      /// </value>
      public DateTime? DateTaken { get; set; }

      private long? _width;
      private long? _height;

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
            _width = value.Width;
            _height = value.Height;
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

            model.DateTaken = DateTaken;
            model.Width = Resolution.Width;
            model.Height = Resolution.Height;

            return model;
         }

         set
         {
            if (value == null) return;

            base.Model = value;

            if (value.DateTaken.HasValue)
            {
               DateTaken = value.DateTaken.Value;
            }

            Resolution = new(value.Width, value.Height);
         }
      }

      #endregion // Fields

      #region Constructors

      public PhotoFile(ILogger<MediaContainer> logger,
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
      }

      #endregion // Constructors

      #region Photo File Operations

      public MagickImageInfo? GetImageInfo(string path)
      {
         try
         {
            return new MagickImageInfo(path);
         }
         catch (Exception e)
         {
            Logger.LogDebug("Failed To Retrieve Information For: {}, Because: {}", path, e.Message);
         }

         return null;
      }

      public IExifProfile? GetImageExifProfile(string path)
      {
         try
         {
            using var image = new MagickImage(path);
            return image.GetExifProfile();
         }
         catch (Exception e)
         {
            Logger.LogDebug("Failed To Retrieve Exif Profile For: {}, Because: {}", path, e.Message);
         }

         return null;
      }

      public override void GetFileInfo(string path, long size)
      {
         if (size == 0)
         {
            DateTaken = null;
            Resolution = new ResolutionType();

            return;
         }

         var info = GetImageInfo(path);

         /*--------------------------------------------------------------------------------
                                             RESOLUTION
         --------------------------------------------------------------------------------*/

         if (info != null)
         {
            Resolution = new ResolutionType(info.Width, info.Height);
         }

         /*--------------------------------------------------------------------------------
                                            EXIF PROFILE
         --------------------------------------------------------------------------------*/

         var profile = GetImageExifProfile(path);

         if (profile != null)
         {
            IExifValue? value = profile.Values.FirstOrDefault(val => val.Tag == ExifTag.DateTimeOriginal) ??
                                profile.Values.FirstOrDefault(val => val.Tag == ExifTag.DateTimeDigitized);

            if (value != null)
            {
               if (DateTime.TryParseExact(value.ToString(), "yyyy:MM:dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
               {
                  DateTaken = date;
               }
            }
         }
      }

      public byte[]? Preview(int width = 0, int height = 0, MagickFormat format = MagickFormat.Jpeg)
      {
         byte[]? output = null;

         if (FullPath is null) return null;

         using (var image = new MagickImage(FullPath))
         {
            try
            {
               // Convert the photo's format in case the encoder is missing as it is in case of HEIC files
               image.Format = format;

               image.AutoOrient();

               if ((width > 0) || (height > 0))
               {
                  var size = new MagickGeometry((uint)width, (uint)height)
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
               Logger.LogWarning("Failed To Resize: {}, Because: {}", FullPath, e.Message);
               Logger.LogDebug("{}", e.ToString());
            }
         }

         return output;
      }

      private byte[]? GenerateThumbnail(string path, int width, int height, bool crop)
      {
         byte[]? output = null;

         using (var image = new MagickImage(path))
         {
            try
            {
               var size = new MagickGeometry((width > 0) ? (uint)(width) : image.BaseWidth, (height > 0) ? (uint)(height) : image.BaseHeight)
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
            }
            catch (Exception e)
            {
               Logger.LogDebug("Thumbnail Generation Failed For: {}, Because: {}", path, e.Message);
            }
         }

         return output;
      }

      /// <summary>
      /// Overrides the GenerateThumbnails() method of the MediaFile generating thumbnails for the
      /// photo file.
      /// </summary>
      /// <returns>The count of successfully generated thumbnails.</returns>
      public override int GenerateThumbnails(bool force = false)
      {
         int thumbnails = 0, total = 0, generated = 0;
         string[] nullColumns = Thumbnails?.NullColumns ?? [];

         // Make sure the photo file is valid and not corrupted or empty.
         if ((Size == 0) || (Resolution.Height == 0) || (Resolution.Width == 0))
         {
            return thumbnails;
         }

         /*----------------------------------------------------------------------------------
                                         GENERATE THUMBNAILS
         ----------------------------------------------------------------------------------*/

         // Calculate the total number of thumbnails to be generated used for progress reporting
         foreach (var item in Settings.CurrentValue.Thumbnails.Photo)
         {
            total += Math.Min(1, item.Value.Count);
         }

         Progress?.Report(0.0f);

         foreach (var item in Settings.CurrentValue.Thumbnails.Photo)
         {
            string label = item.Key;
            bool crop = item.Value.Crop;
            int count = item.Value.Count;
            int width = item.Value.Width;
            int height = item.Value.Height;

            for (int counter = 0; counter < Math.Max(1, count); counter++)
            {
               byte[]? thumbnail = null;
               string column = (count >= 1) ? string.Format("{0}{1}", item.Key, counter) : label;

               if (!force)
               {
                  // Skip the thumbnail generation for this specific thumbnail if it already exists.
                  if (!nullColumns.Contains(column, StringComparer.InvariantCultureIgnoreCase)) continue;
               }

               Logger.LogDebug("Generating The {} Thumbnail For: {}", column, FullPath);

               // Generate the thumbnail.
               if (!string.IsNullOrEmpty(FullPath)) thumbnail = GenerateThumbnail(FullPath, width, height, crop);

               // Report the progress
               Progress?.Report((float)++generated / (float)total);

               if ((thumbnail != null) && (thumbnail.Length > 0))
               {
                  // Add the newly generated thumbnail to the database.
                  if (count >= 1)
                  {
                     if (Thumbnails is not null) Thumbnails[counter] = thumbnail;
                  }
                  else
                  {
                     if (Thumbnails is not null) Thumbnails[label] = thumbnail;
                  }

                  thumbnails++;
               }
            }
         }

         Progress?.Report(1.0f);

         return thumbnails;
      }

      #endregion // Photo File Operations
   }
}