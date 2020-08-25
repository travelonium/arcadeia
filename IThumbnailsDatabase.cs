using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediaCurator
{
   public interface IThumbnailsDatabase
   {
      string FullPath { get; }
      int Maximum { get; }

      int DeleteThumbnails(string id);
      List<string> GetRowIdsList();
      byte[] GetThumbnail(string id, int index);
      Task<byte[]> GetThumbnailAsync(string id, int index, CancellationToken cancellationToken);
      List<byte[]> GetThumbnails(string id);
      Task<List<byte[]>> GetThumbnailsAsync(string id, CancellationToken cancellationToken);
      int GetThumbnailsCount(string id);
      void SetThumbnail(string id, int index, ref byte[] data);
      void Vacuum();
   }
}