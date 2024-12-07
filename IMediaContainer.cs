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
using System.Collections.Generic;

namespace Arcadeia
{
   public interface IMediaContainer : IDisposable
   {
      IMediaContainer Root { get; }
      IMediaContainer? Parent { get; set; }
      string? ParentType { get; set; }
      IEnumerable<MediaContainer> Children { get; }
      string? Id { get; set; }
      string? Name { get; set; }
      string? Description { get; set; }
      string? Type { get; set; }
      string? Path { get; }
      string? FullPath { get; }
      DateTime DateAdded { get; set; }
      DateTime DateCreated { get; set; }
      DateTime DateModified { get; set; }
      MediaContainerFlags? Flags { get; set; }
      Models.MediaContainer Model { get; set; }

      void Delete(bool permanent = false);
      bool Exists();
      MediaContainerType? GetMediaContainerType();
      Type? GetMediaContainerType(string container);
      void Move(string destination);
      bool Save();
   }
}
