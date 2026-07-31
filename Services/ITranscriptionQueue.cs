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

namespace Arcadeia.Services
{
   public interface ITranscriptionQueue
   {
      /// <summary>
      /// Enqueues a media file, identified by its Id, for transcription. A no-op if the same Id is
      /// already queued or being processed.
      /// </summary>
      void Enqueue(string id);

      /// <summary>
      /// Asynchronously enumerates the Ids queued for transcription as they become available.
      /// </summary>
      IAsyncEnumerable<string> DequeueAllAsync(CancellationToken cancellationToken);

      /// <summary>
      /// Marks the transcription of the given Id as finished, allowing it to be enqueued again.
      /// </summary>
      void Complete(string id);
   }
}
