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

namespace Arcadeia
{
   public class OrderedAsyncProgress<T>(Func<T, Task> handler) : IProgress<T>
   {
      private readonly Func<T, Task> _handler = handler ?? throw new ArgumentNullException(nameof(handler));
      private readonly SemaphoreSlim _semaphore = new(1, 1);

      public void Report(T value)
      {
         // Queue the execution to ensure ordered processing
         _ = ProcessAsync(value);
      }

      private async Task ProcessAsync(T value)
      {
         await _semaphore.WaitAsync();
         try
         {
            await _handler(value);
         }
         finally
         {
            _semaphore.Release();
         }
      }
   }
}
