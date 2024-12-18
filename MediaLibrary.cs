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

using System.Diagnostics;
using Arcadeia.Configuration;
using Microsoft.Extensions.Options;

namespace Arcadeia
{
   class MediaLibrary : MediaContainer, IMediaLibrary
   {
      private readonly object _lock = new();

      // A simple dictionary caching the newly created Ids keyed by the FullPath of their holders.
      // This makes parallel scanning possible as otherwise concurrency issues with updating the
      // Solr index leads to duplicate entries. The cache should be cleared once the scanning is over.
      private readonly Dictionary<string, string> _cache = new();

      #region Constructors

      /// <summary>
      /// Initializes a new instance of the <see cref="Arcadeia.MediaLibrary"/> class.
      /// </summary>
      public MediaLibrary(ILogger<MediaContainer> logger,
                          IServiceProvider services,
                          IOptionsMonitor<Settings> settings,
                          IThumbnailsDatabase thumbnailsDatabase,
                          string? id = null, string? path = null,
                          IProgress<float>? progress = null
      ) : base(logger, services, settings, thumbnailsDatabase, null, id, path, progress)
      {
         if (Created)
         {
            DateCreated = DateTime.UtcNow;

            if (Save())
            {
               Created = false;
               Modified = false;
            }
         }
      }

      #endregion // Constructors

      #region Public Methods

      public string? GenerateUniqueId(string? path, out bool reused)
      {
         lock (_lock)
         {
            if (path == null)
            {
               reused = false;
               return null;
            }

            if (_cache.TryGetValue(path, out string? cached))
            {
               reused = true;
               return cached;
            }
            else
            {
               reused = false;
               _cache[path] = System.IO.Path.GetRandomFileName();
            }

            return _cache[path];
         }
      }

      public void ClearCache()
      {
         _cache.Clear();

         Logger.LogInformation("Media Library Cache Cleared!");
      }

      public MediaFile? InsertMediaFile(string path, IProgress<float>? progress = null)
      {
         MediaFile? mediaFile = null;
         MediaContainerType mediaType = GetMediaType(path);

         switch (mediaType)
         {
            /*-------------------------------------------------------------------------------------
                                                AUDIO FILE
            -------------------------------------------------------------------------------------*/

            case MediaContainerType.Audio:
               /* mediaFile = new AudioFile(Logger, Services, Configuration, ThumbnailsDatabase, MediaLibrary, path: path, progress: progress); */
               break;

            /*-------------------------------------------------------------------------------------
                                                VIDEO FILE
            -------------------------------------------------------------------------------------*/

            case MediaContainerType.Video:
               mediaFile = new VideoFile(Logger, Services, Settings, ThumbnailsDatabase, MediaLibrary, path: path, progress: progress);
               break;

            /*-------------------------------------------------------------------------------------
                                                PHOTO FILE
            -------------------------------------------------------------------------------------*/

            case MediaContainerType.Photo:
               mediaFile = new PhotoFile(Logger, Services, Settings, ThumbnailsDatabase, MediaLibrary, path: path, progress: progress);
               break;

            /*-------------------------------------------------------------------------------------
                                                 UNKNOWN
            -------------------------------------------------------------------------------------*/

            case MediaContainerType.Unknown:
               break;

            default:
               break;
         }

         return mediaFile;
      }

      public MediaFolder InsertMediaFolder(string path)
      {
         return new MediaFolder(Logger, Services, Settings, ThumbnailsDatabase, MediaLibrary, path: path);
      }

      /// <summary>
      /// Checks an already present element in the MediaLibrary against its physical form to see if
      /// anything has changed and if the element needs to be updated or deleted.
      /// </summary>
      public MediaContainer? UpdateMediaContainer(string? id = null, string? type = null, string? path = null)
      {
         Debug.Assert(((id != null) && (type != null)) || (path != null));
         Debug.Assert(((id == null) && (type == null)) || (path == null));

         MediaContainer mediaContainer;

         if (path != null)
         {
            mediaContainer = new(Logger, Services, Settings, ThumbnailsDatabase, MediaLibrary, id: null, path: path);

            if (string.IsNullOrEmpty(mediaContainer.Type))
            {
               throw new ArgumentNullException(mediaContainer.Type, string.Format("Failed to determine the MediaContainer type: {0}", mediaContainer.FullPath));
            }

            if (string.IsNullOrEmpty(mediaContainer.Id))
            {
               throw new ArgumentNullException(mediaContainer.Id, string.Format("Failed to determine the MediaContainer id: {0}", mediaContainer.FullPath));
            }

            id = mediaContainer.Id;
            type = mediaContainer.Type;
         }

         switch (type?.ToEnum<MediaContainerType>() ?? MediaContainerType.Unknown)
         {
            case MediaContainerType.Audio:
               throw new NotImplementedException("Audio files cannot yet be handled!");

            case MediaContainerType.Video:
               mediaContainer = new VideoFile(Logger, Services, Settings, ThumbnailsDatabase, MediaLibrary, id: id);
               break;

            case MediaContainerType.Photo:
               mediaContainer = new PhotoFile(Logger, Services, Settings, ThumbnailsDatabase, MediaLibrary, id: id);
               break;

            case MediaContainerType.Drive:
               mediaContainer = new MediaDrive(Logger, Services, Settings, ThumbnailsDatabase, MediaLibrary, id: id);
               break;

            case MediaContainerType.Server:
               mediaContainer = new MediaServer(Logger, Services, Settings, ThumbnailsDatabase, MediaLibrary, id: id);
               break;

            case MediaContainerType.Folder:
               mediaContainer = new MediaFolder(Logger, Services, Settings, ThumbnailsDatabase, MediaLibrary, id: id);
               break;

            default:
               return null;
         }

         return mediaContainer;
      }

      public MediaContainerType GetMediaType(string path)
      {
         // Extract the file extension including the '.' character.
         string fileExtension = System.IO.Path.GetExtension(path).ToLower();

         if ((fileExtension == null) || (fileExtension.Length == 0))
         {
            // The media type is not recognized as it has an invalid or no extensions.
            return MediaContainerType.Unknown;
         }

         // It appears that the file does have an extension. We may proceed.

         // Check if it's a recognized video format.
         if (Settings.CurrentValue.SupportedExtensions.Video.Contains(fileExtension))
         {
            // Looks like the file is a recognized video format.
            return MediaContainerType.Video;
         }

         // Check if it's a recognized photo format.
         if (Settings.CurrentValue.SupportedExtensions.Photo.Contains(fileExtension))
         {
            // Looks like the file is a recognized photo format.
            return MediaContainerType.Photo;
         }

         // Check if it's a recognized audio format.
         if (Settings.CurrentValue.SupportedExtensions.Audio.Contains(fileExtension))
         {
            // Looks like the file is a recognized audio format.
            return MediaContainerType.Audio;
         }

         // Unrecognized file format.
         return MediaContainerType.Unknown;
      }

      #endregion // Public Methods
   }
}
