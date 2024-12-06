﻿using Microsoft.Extensions.Options;
using MediaCurator.Configuration;

namespace MediaCurator
{
   public class MediaServer : MediaContainer
   {
      #region Fields

      #endregion // Fields

      #region Constructors

      public MediaServer(ILogger<MediaContainer> logger,
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
   }
}