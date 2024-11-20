using System.Diagnostics;

namespace MediaCurator
{
   class MediaLibrary : MediaContainer, IMediaLibrary
   {
      private readonly object _lock = new();

      // A simple dictionary caching the newly created Ids keyed by the FullPath of their holders.
      // This makes parallel scanning possible as otherwise concurrency issues with updating the
      // Solr index leads to duplicate entries. The cache should be cleared once the scanning is over.
      private readonly Dictionary<string, string> _cache = new();

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
                          string? id = null, string? path = null,
                          IProgress<float>? progress = null
      ) : base(logger, services, configuration, thumbnailsDatabase, null, id, path, progress)
      {
         // Read and store the supported extensions from the configuration file.
         SupportedExtensions = new SupportedExtensions(Configuration);

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
               mediaFile = new VideoFile(Logger, Services, Configuration, ThumbnailsDatabase, MediaLibrary, path: path, progress: progress);
               break;

            /*-------------------------------------------------------------------------------------
                                                PHOTO FILE
            -------------------------------------------------------------------------------------*/

            case MediaContainerType.Photo:
               mediaFile = new PhotoFile(Logger, Services, Configuration, ThumbnailsDatabase, MediaLibrary, path: path, progress: progress);
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
         return new MediaFolder(Logger, Services, Configuration, ThumbnailsDatabase, MediaLibrary, path: path);
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
            mediaContainer = new(Logger, Services, Configuration, ThumbnailsDatabase, MediaLibrary, id: null, path: path);

            if (string.IsNullOrEmpty(mediaContainer.Type))
            {
               throw new ArgumentNullException(mediaContainer.Type, String.Format("Failed to determine the MediaContainer type: {0}", mediaContainer.FullPath));
            }

            if (string.IsNullOrEmpty(mediaContainer.Id))
            {
               throw new ArgumentNullException(mediaContainer.Id, String.Format("Failed to determine the MediaContainer id: {0}", mediaContainer.FullPath));
            }

            id = mediaContainer.Id;
            type = mediaContainer.Type;
         }

         switch (type?.ToEnum<MediaContainerType>() ?? MediaContainerType.Unknown)
         {
            case MediaContainerType.Audio:
               throw new NotImplementedException("Audio files cannot yet be handled!");

            case MediaContainerType.Video:
               mediaContainer = new VideoFile(Logger, Services, Configuration, ThumbnailsDatabase, MediaLibrary, id: id);
               break;

            case MediaContainerType.Photo:
               mediaContainer = new PhotoFile(Logger, Services, Configuration, ThumbnailsDatabase, MediaLibrary, id: id);
               break;

            case MediaContainerType.Drive:
               mediaContainer = new MediaDrive(Logger, Services, Configuration, ThumbnailsDatabase, MediaLibrary, id: id);
               break;

            case MediaContainerType.Server:
               mediaContainer = new MediaServer(Logger, Services, Configuration, ThumbnailsDatabase, MediaLibrary, id: id);
               break;

            case MediaContainerType.Folder:
               mediaContainer = new MediaFolder(Logger, Services, Configuration, ThumbnailsDatabase, MediaLibrary, id: id);
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

      #endregion // Public Methods
   }
}
