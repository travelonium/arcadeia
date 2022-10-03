using System;
using System.Collections.Generic;

namespace MediaCurator
{
   public interface IMediaContainer : IDisposable
   {
      IMediaContainer Root { get; }
      IMediaContainer Parent { get; set; }
      string ParentType { get; set; }
      IEnumerable<MediaContainer> Children { get; }
      string Id { get; set; }
      string Name { get; set; }
      string Description { get; set; }
      string Type { get; set; }
      string Path { get; }
      string FullPath { get; }
      DateTime DateAdded { get; set; }
      DateTime DateCreated { get; set; }
      DateTime DateModified { get; set; }
      MediaContainerFlags Flags { get; set; }
      Models.MediaContainer Model { get; set; }

      void Delete(bool permanent = false);
      bool Exists();
      MediaContainerType GetMediaContainerType();
      Type GetMediaContainerType(string container);
      void Move(string destination);
      bool Save();
   }
}
