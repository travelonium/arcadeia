using System;
using System.Xml.Linq;
using System.Collections.Generic;

namespace MediaCurator
{
   public interface IMediaLibrary : IMediaContainer
   {
      MediaFile InsertMedia(string path);
      MediaContainer UpdateMediaContainer(string id = null, string type = null, string path = null);
      string GenerateUniqueId(string path, out bool reused);
      void ClearCache();
   }
}