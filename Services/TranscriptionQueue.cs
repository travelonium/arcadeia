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

using System.Collections.Concurrent;
using System.Threading.Channels;

namespace Arcadeia.Services
{
   /// <summary>
   /// A minimal, independently-consumable queue of media file Ids awaiting transcription. Deliberately
   /// separate from the IBackgroundTaskQueue used by the ScannerService, which serializes its tasks
   /// through a single consumer and debounces by key for FileSystemWatcher event coalescing, neither
   /// of which is appropriate for low-concurrency, CPU-bound transcription work.
   /// </summary>
   public class TranscriptionQueue : ITranscriptionQueue
   {
      private readonly Channel<string> _channel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
      {
         SingleReader = false,
         SingleWriter = false,
      });

      // Tracks Ids that are either sitting in the channel or actively being transcribed, so the same
      // file isn't enqueued twice (e.g. touched by both a scan and a FileSystemWatcher event).
      private readonly ConcurrentDictionary<string, byte> _queued = new();

      public void Enqueue(string id)
      {
         if (string.IsNullOrEmpty(id)) return;

         if (_queued.TryAdd(id, 0))
         {
            _channel.Writer.TryWrite(id);
         }
      }

      public IAsyncEnumerable<string> DequeueAllAsync(CancellationToken cancellationToken)
      {
         return _channel.Reader.ReadAllAsync(cancellationToken);
      }

      public void Complete(string id)
      {
         _queued.TryRemove(id, out _);
      }
   }
}
