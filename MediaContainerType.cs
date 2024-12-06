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
