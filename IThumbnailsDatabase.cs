﻿using System.Collections.Generic;
using System.Data.SQLite;
using System.Threading;
using System.Threading.Tasks;

namespace MediaCurator
{
   public interface IThumbnailsDatabase
   {
      string Path { get; }
      string FullPath { get; }
      int Maximum { get; }

      int DeleteThumbnails(string id);
      byte[] GetThumbnail(string id, int index);
      byte[] GetThumbnail(string id, string label);
      Task<byte[]> GetThumbnailAsync(string id, int index, CancellationToken cancellationToken);
      Task<byte[]> GetThumbnailAsync(string id, string label, CancellationToken cancellationToken);
      int GetThumbnailsCount(string id);
      void SetThumbnail(string id, int index, ref byte[] data);
      void SetThumbnail(string id, string label, ref byte[] data);
      void SetJournalMode(SQLiteJournalModeEnum mode);
      void Checkpoint(string argument = "TRUNCATE");
      void Vacuum();
   }
}