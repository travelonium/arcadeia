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

using Arcadeia.Configuration;
using Microsoft.Extensions.Options;

namespace Arcadeia.Services
{
   public class FileSystemService(ILoggerFactory loggerFactory,
                                  ILogger<FileSystemService> logger,
                                  IOptionsMonitor<Settings> settings,
                                  IHostApplicationLifetime applicationLifetime) : IFileSystemService
   {
      protected readonly IOptionsMonitor<Settings> _settings = settings;

      private readonly ILogger<FileSystemService> _logger = logger;

      private readonly ILoggerFactory _loggerFactory = loggerFactory;

      private readonly CancellationToken _cancellationToken = applicationLifetime.ApplicationStopping;

      private readonly SemaphoreSlim _semaphore = new(1, 1);

      public List<FileSystemMount> Mounts
      {
         get; private set;
      } = [];

      #region Constructors

      #endregion // Constructors

      public Task StartAsync(CancellationToken cancellationToken)
      {
         _logger.LogInformation("Starting FileSystem Service...");

         // Try to mount all the configured mounts.
         foreach (var item in _settings.CurrentValue.Mounts)
         {
            try
            {
               var mount = new FileSystemMount(item, _loggerFactory.CreateLogger<FileSystemMount>());

               Mounts.Add(mount);
            }
            catch (Exception e)
            {
               _logger.LogWarning("Failed To Parse Mount: {}", e.Message);
            }
         }

         _logger.LogInformation("FileSystem Service Started.");

         return Task.CompletedTask;
      }

      public Task StopAsync(CancellationToken cancellationToken)
      {
         _logger.LogInformation("Stopping FileSystem Service...");

         // Unmount all the configured mounts.
         foreach (var mount in Mounts)
         {
            mount.Dispose();
         }

         Mounts.Clear();

         _logger.LogInformation("FileSystem Service Stopped.");

         return Task.CompletedTask;
      }

      public async Task RestartAsync(CancellationToken cancellationToken)
      {
         _logger.LogInformation("Restarting FileSystem Service...");

         // Ensure only one restart happens at a time.
         await _semaphore.WaitAsync(cancellationToken);

         try
         {
            // Stop the service
            await StopAsync(cancellationToken);

            // Start the service
            await StartAsync(cancellationToken);

            _logger.LogInformation("FileSystem Service Restarted.");
         }
         catch (Exception ex)
         {
            _logger.LogError("Failed To Restart FileSystem Service: {}", ex.Message);
         }
         finally
         {
            _semaphore.Release();
         }
      }

      public async Task ReloadAsync(CancellationToken cancellationToken)
      {
         _logger.LogInformation("Reloading FileSystem Service...");

         // Ensure only one restart happens at a time.
         await _semaphore.WaitAsync(cancellationToken);

         try
         {
            foreach (var item in _settings.CurrentValue.Mounts)
            {
               var mount = Mounts.Where(x => x.Folder == item.Folder).FirstOrDefault();
               if (mount is not null)
               {
                  if (mount.Types != item.Types || mount.Device != item.Device || mount.Options != item.Options || mount.Attached)
                  {
                     // Remount it.
                  }
               }
               else
               {
                  // We have a new mount.
               }
            }

            _logger.LogInformation("FileSystem Service Restarted.");
         }
         catch (Exception ex)
         {
            _logger.LogError("Failed To Restart FileSystem Service: {}", ex.Message);
         }
         finally
         {
            _semaphore.Release();
         }
      }
   }
}
