using System;

namespace MediaCurator
{
   public interface IMediaContainer : IDisposable
   {
      IMediaContainer Root { get; }
      public IMediaContainer Parent { get; set; }
      string Id { get; set; }
      string Name { get; set; }
      string Type { get; }
      string FullPath { get; }
      MediaContainerFlags Flags { get; set; }
      Models.MediaContainer Model { get; set; }

      bool Save();
      bool Exists();
      void Delete(bool permanent = false);
      MediaContainerType GetMediaContainerType();
   }
}