using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Security.Permissions;
using System.Linq;
using System.IO;
using System.Diagnostics;

namespace MediaCurator.Services
{
   public class FileSystemService(IConfiguration configuration,
                                  ILogger<FileSystemService> logger,
                                  IHostApplicationLifetime applicationLifetime) : IFileSystemService
   {
      protected readonly IConfiguration _configuration = configuration;

      private readonly ILogger<FileSystemService> _logger = logger;

      private readonly CancellationToken _cancellationToken = applicationLifetime.ApplicationStopping;

      private Lazy<List<FileSystemMount>> _mounts => new(() =>
      {
         List<FileSystemMount> mounts = [];

         foreach (var item in _configuration.GetSection("Mounts").Get<List<Dictionary<string, string>>>() ?? [])
         {
            try
            {
               FileSystemMount mount = new(item);

               mounts.Add(mount);
            }
            catch (Exception e)
            {
               _logger.LogWarning("Failed To Parse Mount: {}", e.Message);
            }
         }

         return mounts;
      });

      public List<FileSystemMount> Mounts => _mounts.Value;

        #region Constructors

        #endregion // Constructors

        public Task StartAsync(CancellationToken cancellationToken)
      {
         _logger.LogInformation("Starting FileSystem Service...");

         // Try to mount all the configured mounts.
         foreach (var mount in Mounts)
         {
            try
            {
               mount.Attach();

               _logger.LogInformation("Mounted: {} @ {}", mount.Device, mount.Folder);
            }
            catch (Exception e)
            {
               _logger.LogError("Failed To Mount: {} Because: {}", mount.Device, e.Message);
            }
         }

         _logger.LogInformation("FileSystem Service Started.");

         return Task.CompletedTask;
      }

      public Task StopAsync(CancellationToken cancellationToken)
      {
         // Unmount all the configured mounts.
         foreach (var mount in Mounts)
         {
            try
            {
               mount.Detach();

               _logger.LogInformation("Unmounted: {} @ {}", mount.Device, mount.Folder);
            }
            catch (Exception e)
            {
               _logger.LogError("Failed To Unmount: {} Because: {}", mount.Device, e.Message);
            }
         }

         _logger.LogInformation("FileSystem Service Stopped.");

         return Task.CompletedTask;
      }

      public void Dispose()
      {
      }
   }
}
