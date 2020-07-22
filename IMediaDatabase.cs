using System;
using System.Xml.Linq;

namespace MediaCurator
{
   public interface IMediaDatabase
   {
      string FullPath { get; }

      void UpdateDatabase();
      void UpdateMedia(XElement element, IProgress<Tuple<double, double>> progress, IProgress<byte[]> preview);
      MediaFile InsertMedia(string path, IProgress<Tuple<double, double>> progress, IProgress<byte[]> preview);
   }
}