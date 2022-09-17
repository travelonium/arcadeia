using System;
using System.Collections.Generic;

namespace MediaCurator
{
   public interface IMediaContainer : IDisposable
   {
      IMediaContainer Root { get; }
      IMediaContainer Parent { get; set; }
      IEnumerable<MediaContainer> Children { get; }
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