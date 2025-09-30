/*
 *  Copyright © 2024 Travelonium AB
 *
 *  This file is part of Arcadeia.
 *
 *  Arcadeia is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU Affero General Public License as published
 *  by the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  Arcadeia is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU Affero General Public License for more details.
 *
 *  You should have received a copy of the GNU Affero General Public License
 *  along with Arcadeia. If not, see <https://www.gnu.org/licenses/>.
 *
 */

using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;

namespace Arcadeia
{
   public interface IThumbnailsDatabase
   {
      string Path { get; }
      string FullPath { get; }
      int Count { get; }

      void Create(string id);
      bool Exists(string id);
      List<string> GetIds();
      int DeleteThumbnails(string id);
      byte[] GetThumbnail(string id, int index);
      byte[] GetThumbnail(string id, string label);
      Task<byte[]> GetThumbnailAsync(string id, int index, CancellationToken cancellationToken);
      Task<byte[]> GetThumbnailAsync(string id, string label, CancellationToken cancellationToken);
      int GetThumbnailsCount(string id);
      string[] GetNullColumns(string id);
      void SetThumbnail(string id, int index, ref byte[] data);
      void SetThumbnail(string id, string label, ref byte[] data);
      void SetJournalMode(string mode);
      void Checkpoint(string argument = "TRUNCATE");
      void Vacuum();
   }
}