using System;
using System.Xml.Linq;
using System.Collections.Generic;

namespace MediaCurator
{
   public interface IMediaLibrary : IMediaContainer
   {
      MediaFile InsertMedia(string path);
      MediaFile UpdateMedia(string id = null, string path = null);
   }
}