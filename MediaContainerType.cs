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

namespace Arcadeia
{
   public enum MediaContainerType
   {
      Unknown,
      Library,
      Drive,
      Server,
      Folder,
      Audio,
      Video,
      Photo
   }

   static class MediaContainerTypeExtensions
   {
      public static Type? ToType(this MediaContainerType type)
      {
         switch (type)
         {
            case MediaContainerType.Library:
               return typeof(MediaLibrary);
            case MediaContainerType.Drive:
               return typeof(MediaDrive);
            case MediaContainerType.Server:
               return typeof(MediaServer);
            case MediaContainerType.Folder:
               return typeof(MediaFolder);
            case MediaContainerType.Video:
               return typeof(VideoFile);
            case MediaContainerType.Photo:
               return typeof(PhotoFile);
            case MediaContainerType.Unknown:
               break;
            case MediaContainerType.Audio:
               // return typeof(AudioFile);
               break;
         }

         return null;
      }

      public static MediaContainerType ToMediaContainerType(this Type type)
      {
         if (type.ToString() == typeof(MediaLibrary).ToString())
         {
            return MediaContainerType.Library;
         }

         if (type.ToString() == typeof(MediaDrive).ToString())
         {
            return MediaContainerType.Drive;
         }

         if (type.ToString() == typeof(MediaServer).ToString())
         {
            return MediaContainerType.Server;
         }

         if (type.ToString() == typeof(MediaFolder).ToString())
         {
            return MediaContainerType.Folder;
         }

         if (type.ToString() == typeof(VideoFile).ToString())
         {
            return MediaContainerType.Video;
         }

         if (type.ToString() == typeof(PhotoFile).ToString())
         {
            return MediaContainerType.Photo;
         }

         /*
         if (type.ToString() == typeof(AudioFile).ToString())
         {
            return MediaContainerType.Audio;
         }
         */

         return MediaContainerType.Unknown;
      }

      public static T ToEnum<T>(this string name)
      {
         return (T) Enum.Parse(typeof(T), name, true);
      }
   }
}
