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

using System;
using System.Xml.Linq;
using System.Collections.Generic;

namespace Arcadeia
{
   public interface IMediaLibrary : IMediaContainer
   {
      MediaFile? InsertMediaFile(string path, IProgress<float>? progress = null);
      MediaFolder InsertMediaFolder(string path);
      MediaContainer? UpdateMediaContainer(string? id = null, string? type = null, string? path = null);
      string? GenerateUniqueId(string? path, out bool reused);
      MediaContainerType GetMediaType(string path);
      void ClearCache();
   }
}