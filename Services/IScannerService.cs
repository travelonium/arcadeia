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
   public interface IScannerService: IHostedService
   {
      bool Updating { get; }

      bool Scanning { get; }

      bool Cleaning { get; }

      public Task RestartAsync(CancellationToken cancellationToken);

      public Task ScanAsync(string uuid, string path, string type, CancellationToken cancellationToken);

      public Task UpdateAsync(string uuid, CancellationToken cancellationToken);

      public Task CleanupAsync(string uuid, CancellationToken cancellationToken);

      /// <summary>Queues an immediate scan of every available folder, the same as StartupScan.</summary>
      public void QueueScan();

      /// <summary>Queues an immediate library update, the same as StartupUpdate.</summary>
      public void QueueUpdate();

      /// <summary>Queues an immediate thumbnails database cleanup, the same as StartupCleanup.</summary>
      public void QueueCleanup();
   }
}
