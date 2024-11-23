using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace MediaCurator
{
   public class MediaDrive : MediaContainer
   {
      #region Fields

      #endregion // Fields

      #region Constructors

      public MediaDrive(ILogger<MediaContainer> logger,
                        IServiceProvider services,
                        IConfiguration configuration,
                        IThumbnailsDatabase thumbnailsDatabase,
                        IMediaLibrary mediaLibrary,
                        string? id = null, string? path = null,
                        IProgress<float>? progress = null
      ) : base(logger, services, configuration, thumbnailsDatabase, mediaLibrary, id, path, progress)
      {
         // The base class constructor will take care of the entry, its general attributes and its
         // parents and below we'll take care of its specific attributes.

         if (Skipped) return;
      }

      #endregion // Constructors
   }
}