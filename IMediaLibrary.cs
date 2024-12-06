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