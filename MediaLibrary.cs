using System;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MediaCurator.Solr;
using Microsoft.Extensions.DependencyInjection;

namespace MediaCurator
{
   class MediaLibrary : MediaContainer, IMediaLibrary
   {
      /// <summary>
      /// The supported file extensions for each media type.
      /// </summary>
      public readonly SupportedExtensions SupportedExtensions;

      #region Constructors

      /// <summary>
      /// Initializes a new instance of the <see cref="MediaCurator.MediaLibrary"/> class.
      /// </summary>
      public MediaLibrary(ILogger<MediaContainer> logger,
                          IServiceProvider services,
                          IConfiguration configuration,
                          IThumbnailsDatabase thumbnailsDatabase,
                          string id = null, string path = null
      ) : base(logger, services, configuration, thumbnailsDatabase, null, id, path)
      {
         // Read and store the supported extensions from the configuration file.
         SupportedExtensions = new SupportedExtensions(Configuration);

         if (Created)
         {
            if (Save())
            {
               Created = false;
               Modified = false;
            }
         }
      }

      #endregion // Constructors

      public MediaFile InsertMedia(string path)
      {
         MediaFile mediaFile = null;
         MediaContainerType mediaType = GetMediaType(path);

         switch (mediaType)
         {
            /*-------------------------------------------------------------------------------------
                                                 UNKNOWN
            -------------------------------------------------------------------------------------*/

            case MediaContainerType.Unknown:

               break;

            /*-------------------------------------------------------------------------------------
                                                AUDIO FILE
            -------------------------------------------------------------------------------------*/

            case MediaContainerType.Audio:

               /*mediaFile = */ InsertAudioFile(path);

               break;

            /*-------------------------------------------------------------------------------------
                                                VIDEO FILE
            -------------------------------------------------------------------------------------*/

            case MediaContainerType.Video:

               mediaFile = InsertVideoFile(path);

               break;

            /*-------------------------------------------------------------------------------------
                                                PHOTO FILE
            -------------------------------------------------------------------------------------*/

            case MediaContainerType.Photo:

               mediaFile = InsertPhotoFile(path);

               break;
         }

         return mediaFile;
      }

      /// <summary>
      /// Checks an already present element in the MediaLibrary against its physical file to see if
      /// anything has changed and if the element needs to be updated or deleted.
      /// </summary>
      /// <param name="model">The media entry to be checked for changes.</param>
      public MediaFile UpdateMedia(string id = null, string path = null)
      {
         Debug.Assert((id != null) || (path != null));
         Debug.Assert((id == null) || (path == null));

         // Instantiate a MediaFile using the given id or path.
         MediaFile mediaFile = new(Logger, Services, Configuration, ThumbnailsDatabase, MediaLibrary, id: id, path: path);

         switch (mediaFile.GetMediaContainerType())
         {
            /*-------------------------------------------------------------------------------------
                                                AUDIO FILE
            -------------------------------------------------------------------------------------*/

            case MediaContainerType.Audio:

               throw new NotImplementedException("Audio files cannot yet be handled!");

            /*-------------------------------------------------------------------------------------
                                                VIDEO FILE
            -------------------------------------------------------------------------------------*/

            case MediaContainerType.Video:

               mediaFile = UpdateVideoFile(mediaFile.Id);

               break;

            /*-------------------------------------------------------------------------------------
                                                PHOTO FILE
            -------------------------------------------------------------------------------------*/

            case MediaContainerType.Photo:

               mediaFile = UpdatePhotoFile(mediaFile.Id);

               break;
         }

         return mediaFile;
      }

      private void InsertAudioFile(string path)
      {

      }

      private VideoFile UpdateAudioFile(string id)
      {
         return null;
      }

      private VideoFile InsertVideoFile(string path)
      {
         return new(Logger, Services, Configuration, ThumbnailsDatabase, MediaLibrary, path: path);
      }

      private VideoFile UpdateVideoFile(string id)
      {
         return new(Logger, Services, Configuration, ThumbnailsDatabase, MediaLibrary, id: id);
      }

      private PhotoFile InsertPhotoFile(string path)
      {
         return new(Logger, Services, Configuration, ThumbnailsDatabase, MediaLibrary, path: path);
      }

      private PhotoFile UpdatePhotoFile(string id)
      {
         return new(Logger, Services, Configuration, ThumbnailsDatabase, MediaLibrary, id: id);
      }

      private MediaContainerType GetMediaType(string path)
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
         if (SupportedExtensions[MediaContainerType.Video].Contains(fileExtension))
         {
            // Looks like the file is a recognized video format.
            return MediaContainerType.Video;
         }

         // Check if it's a recognized photo format.         
         if (SupportedExtensions[MediaContainerType.Photo].Contains(fileExtension))
         {
            // Looks like the file is a recognized photo format.
            return MediaContainerType.Photo;
         }

         // Check if it's a recognized audio format.
         if (SupportedExtensions[MediaContainerType.Audio].Contains(fileExtension))
         {
            // Looks like the file is a recognized audio format.
            return MediaContainerType.Audio;
         }

         // Unrecognized file format.
         return MediaContainerType.Unknown;
      }
   }
}
