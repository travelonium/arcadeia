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

using Microsoft.Extensions.Options;
using Arcadeia.Configuration;

namespace Arcadeia
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