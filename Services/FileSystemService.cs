using MediaCurator.Configuration;
using Microsoft.Extensions.Options;

namespace MediaCurator.Services
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
         get
         {
            List<FileSystemMount> mounts = [];

            foreach (var item in _settings.CurrentValue.Mounts)
            {
               try
               {
                  mounts.Add(new FileSystemMount(item, _loggerFactory.CreateLogger<FileSystemMount>()));
               }
               catch (Exception e)
               {
                  _logger.LogWarning("Failed To Parse Mount: {}", e.Message);
               }
            }

            return mounts;
         }
      }

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

      public async Task RestartAsync(CancellationToken cancellationToken)
      {
         _logger.LogInformation("Restarting FileSystem Service...");

         // Ensure only one restart happens at a time.
         await _semaphore.WaitAsync(cancellationToken);

         try
         {
            // Stop the service
            _logger.LogInformation("Stopping FileSystem Service...");
            await StopAsync(cancellationToken);

            // Start the service
            _logger.LogInformation("Starting FileSystem Service...");
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
   }
}
