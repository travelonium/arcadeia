using System;
using System.Xml.Linq;
using System.Collections.Generic;

namespace MediaCurator
{
   public interface IMediaLibrary : IMediaContainer
   {
      void UpdateDatabase();
      MediaFile InsertMedia(string path, IProgress<Tuple<double, double>> progress, IProgress<byte[]> preview);
      MediaFile UpdateMedia(string path, IProgress<Tuple<double, double>> progress, IProgress<byte[]> preview);
      void UpdateMedia(XElement element, IProgress<Tuple<double, double>> progress, IProgress<byte[]> preview);
      List<IMediaContainer> ListMediaContainers(string path, uint flags = 0, uint values = 0);
   }
}