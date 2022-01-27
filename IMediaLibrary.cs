using System;
using System.Xml.Linq;
using System.Collections.Generic;

namespace MediaCurator
{
   public interface IMediaLibrary : IMediaContainer
   {
      void UpdateDatabase();
      MediaFile InsertMedia(string path);
      MediaFile UpdateMedia(string path);
      void UpdateMedia(XElement element);
      List<IMediaContainer> ListMediaContainers(string path, string query = null, uint flags = 0, uint values = 0, bool recursive = false);
   }
}