using System.Xml.Linq;

namespace MediaCurator
{
   public interface IMediaContainer
   {
      IMediaContainer Root { get; }
      public IMediaContainer Parent { get; set; }
      public XElement Self { get; set; }
      string Id { get; set; }
      string Name { get; set; }
      string Type { get; }
      string FullPath { get; }
      MediaContainerFlags Flags { get; set; }
      Models.MediaContainer Model { get; set; }
      string ToolTip { get; }

      void Delete(bool permanent = false, bool deleteXmlEntry = false);
      bool Exists();
      MediaContainerType GetMediaContainerType();
   }
}