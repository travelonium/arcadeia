﻿using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MediaCurator
{
   class MediaServer : MediaContainer
   {
      #region Fields

      #endregion // Fields

      #region Constructors

      public MediaServer(ILogger<MediaContainer> logger,
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

         if (Created)
         {
            // Extract the Server Name from the supplied path removing the \ characters.
            Name = MediaContainer.GetPathComponents(path).Child?.Trim(new Char[] { '\\' });
         }
      }

      #endregion // Constructors
   }
}