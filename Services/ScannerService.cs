using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using MediaCurator.Solr;
using SolrNet;

namespace MediaCurator.Services
{
   public class ScannerService : IHostedService
   {
      private readonly IServiceProvider _services;

      protected readonly IConfiguration _configuration;

      private readonly ILogger<ScannerService> _logger;

      private readonly IMediaLibrary _mediaLibrary;

      private readonly Dictionary<string, FileSystemWatcher> _watchers = new();

      private readonly IBackgroundTaskQueue _taskQueue;

      private readonly CancellationToken _cancellationToken;

      private Timer _periodicScanTimer;

      /// <summary>
      /// The lists of folders to be watched for changes.
      /// </summary>
      public List<string> WatchedFolders
      {
         get
         {
            if (_configuration.GetSection("Scanner:WatchedFolders").Exists())
            {
               return _configuration.GetSection("Scanner:WatchedFolders").Get<List<string>>();
            }
            else
            {
               return new List<string>();
            }
         }
      }

      /// <summary>
      /// The lists of folders to be watched for changes that are mounted or exist.
      /// </summary>
      public List<string> AvailableWatchedFolders
      {
         get
         {
            return WatchedFolders.Where(folder => Directory.Exists(folder)).ToList<string>();
         }
      }

      /// <summary>
      /// Determines whether or not the startup scanning is enabled.
      /// </summary>
      public bool StartupScan
      {
         get
         {
            if (_configuration.GetSection("Scanner:StartupScan").Exists())
            {
               return _configuration.GetSection("Scanner:StartupScan").Get<bool>();
            }
            else
            {
               return false;
            }
         }
      }

      /// <summary>
      /// Determines the periodic scan interval in milliseconds and enables periodic scanning if non-zero.
      /// </summary>
      public long PeriodicScanInterval
      {
         get
         {
            if (_configuration.GetSection("Scanner:PeriodicScanInterval").Exists())
            {
               return _configuration.GetSection("Scanner:PeriodicScanInterval").Get<long>();
            }
            else
            {
               return 0;
            }
         }
      }

      /// <summary>
      /// Determines whether or not the startup update is enabled.
      /// </summary>
      public bool StartupUpdate
      {
         get
         {
            if (_configuration.GetSection("Scanner:StartupUpdate").Exists())
            {
               return _configuration.GetSection("Scanner:StartupUpdate").Get<bool>();
            }
            else
            {
               return false;
            }
         }
      }

      /// <summary>
      /// Determines whether or not the startup cleanup is enabled.
      /// </summary>
      public bool StartupCleanup
      {
         get
         {
            if (_configuration.GetSection("Scanner:StartupCleanup").Exists())
            {
               return _configuration.GetSection("Scanner:StartupCleanup").Get<bool>();
            }
            else
            {
               return false;
            }
         }
      }

      /// <summary>
      /// A list of regex patterns specifying which file names to ignore when scanning.
      /// </summary>
      public List<String> IgnoredFileNames
      {
         get
         {
            if (_configuration.GetSection("Scanner:IgnoredFileNames").Exists())
            {
               return _configuration.GetSection("Scanner:IgnoredFileNames").Get<List<String>>();
            }
            else
            {
               return new List<String>();
            }
         }
      }

      public ScannerService(ILogger<ScannerService> logger,
                            IServiceProvider services,
                            IConfiguration configuration,
                            IBackgroundTaskQueue taskQueue,
                            IHostApplicationLifetime applicationLifetime,
                            IMediaLibrary mediaLibrary)
      {
         _logger = logger;
         _services = services;
         _taskQueue = taskQueue;
         _mediaLibrary = mediaLibrary;
         _configuration = configuration;
         _cancellationToken = applicationLifetime.ApplicationStopping;
      }

      public Task StartAsync(CancellationToken cancellationToken)
      {
         foreach (string folder in WatchedFolders)
         {
            if (!Directory.Exists(folder))
            {
               _logger.LogWarning("Watched Folder Unavailable: {}", folder);

               continue;
            }

            try
            {
               var watcher = new FileSystemWatcher
               {
                  // Set the watched path.
                  Path = folder,

                  // Include the subdirectories.
                  IncludeSubdirectories = true,

                  // Configure the filters.
                  NotifyFilter = NotifyFilters.CreationTime
                                    | NotifyFilters.LastWrite
                                    | NotifyFilters.FileName
                  // | NotifyFilters.DirectoryName
               };

               // Add event handlers.
               watcher.Error += OnError;
               watcher.Created += OnCreated;
               watcher.Changed += OnChanged;
               watcher.Deleted += OnChanged;
               watcher.Renamed += OnRenamed;

               // Begin watching.
               watcher.EnableRaisingEvents = true;

               // Add the watcher to the list of watchers.
               _watchers.Add(folder, watcher);

               _logger.LogInformation("Started Watching: {}", folder);
            }
            catch (Exception e)
            {
               _logger.LogWarning("Failed To Watch: {}, Because: {}", folder, e.Message);
            }
         }

         // Start the startup scanning task if necessary.
         if (StartupScan)
         {
            _logger.LogInformation("Starting Startup Scanning...");

            foreach (var folder in WatchedFolders)
            {
               // Queue the folder startup folder scan.
               _taskQueue.QueueBackgroundTask(folder, cancellationToken =>
               {
                  return Task.Run(() => Scan(folder, "Startup"), cancellationToken);
               });
            }
         }

         // Schedule the periodic scanning task if necessary.
         if (PeriodicScanInterval > 0)
         {
            _logger.LogInformation("Configuring Periodic Scanning...");

            _periodicScanTimer = new Timer(state =>
            {
               foreach (var folder in AvailableWatchedFolders)
               {
                  // Queue the folder periodic scan.
                  _taskQueue.QueueBackgroundTask(folder, cancellationToken =>
                  {
                     return Task.Run(() => Scan(folder, "Periodic"), cancellationToken);
                  });
               }
            }, null, PeriodicScanInterval, PeriodicScanInterval);
         }

         // Start the startup update task if necessary.
         if (StartupUpdate)
         {
            // Queue the startup update task.
            _taskQueue.QueueBackgroundTask("Startup Update", cancellationToken =>
            {
               return Task.Run(() => Update(_cancellationToken), cancellationToken);
            });

            _logger.LogInformation("Startup Update Queued.");
         }

         // Start the background task processor.
         Task.Run(() => BackgroundTaskProcessorAsync(_cancellationToken).Wait(), cancellationToken);

         _logger.LogInformation("Scanner Service Started.");

         return Task.CompletedTask;
      }

      private async Task BackgroundTaskProcessorAsync(CancellationToken cancellationToken)
      {
         _logger.LogInformation("Scanner Service Background Task Processor Started.");

         while (!cancellationToken.IsCancellationRequested)
         {
            var task = await _taskQueue.DequeueAsync(cancellationToken);

            try
            {
               await task(cancellationToken);
            }
            catch (Exception e)
            {
               _logger.LogError(e, "Error Executing: {}", nameof(task));
            }
         }

         _logger.LogInformation("Scanner Service Background Task Processor Stopped.");
      }

      private void Scan(string path, string type)
      {
         var patterns = IgnoredFileNames.Select(pattern => new Regex(pattern, RegexOptions.IgnoreCase)).ToList<Regex>();

         _logger.LogInformation("{} Scanning Started: {}", type, path);

         try
         {
            var files = Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories);

            // Loop through the files in the specific MediaLocation.
            foreach (var file in files)
            {
               var name = Path.GetFileName(file);

               if (_cancellationToken.IsCancellationRequested)
               {
                  // Return gracefully now!
                  return;
               }

               // Should the file name be ignored
               foreach (Regex pattern in patterns)
               {
                  if (pattern.IsMatch(name))
                  {
                     _logger.LogDebug("File Ignored: {}", file);

                     goto Skip;
                  }
               }

               try
               {
                  // Add the file to the MediaLibrary.
                  using MediaFile newMediaFile = _mediaLibrary.InsertMedia(file);
               }
               catch (Exception e)
               {
                  _logger.LogWarning("Failed To Insert: {}, Because: {}", file, e.Message);
                  _logger.LogDebug("{}", e.ToString());

                  goto Skip;
               }

            Skip:
               continue;
            }

            _logger.LogInformation("{} Scanning Finished: {}", type, path);
         }
         catch (System.IO.DirectoryNotFoundException)
         {
            // This is strange. A location previously specified seems to have been deleted. We can all but ignore it.
            _logger.LogWarning("Path Unavailable: {}", path);

            return;
         }
         catch (System.UnauthorizedAccessException e)
         {
            // We probably do not have access to the folder. Let's ignore this one and move on.
            _logger.LogWarning("{} Scanning Failed: {}, Because: {}", type, path, e.Message);

            return;
         }
      }

      private void Update(CancellationToken cancellationToken)
      {
         _logger.LogInformation("Startup Update Started.");

         // It's now time to go through the MediaLibrary itself and check for changes on the disk. 

         // Consume the scoped Solr Index Service.
         using IServiceScope scope = _services.CreateScope();
         ISolrIndexService<Models.MediaContainer> solrIndexService = scope.ServiceProvider.GetRequiredService<ISolrIndexService<Models.MediaContainer>>();

         // Enumerate all the media files from the MediaLibrary database. This is used to detect
         // whether or not any files have been physically removed and thus update the MediaLibrary.

         var documents = solrIndexService.Get(SolrQuery.All);

         _logger.LogInformation("Updating {} Media Library Entries...", documents.Count);

         // Loop through the mediaFiles.
         foreach (var document in documents)
         {
            if (cancellationToken.IsCancellationRequested)
            {
               // Return gracefully now!
               return;
            }

            try
            {
               if (!String.IsNullOrEmpty(document.Id) && !String.IsNullOrEmpty(document.FullPath))
               {
                  // Make sure if the path is located in a watched folder, that folder is available.
                  if (!WatchedFolders.Any(folder => document.FullPath.StartsWith(folder)) ||
                      AvailableWatchedFolders.Any(folder => document.FullPath.StartsWith(folder)))
                  {
                     // Update the current media container.
                     using MediaContainer mediaContainer = _mediaLibrary.UpdateMediaContainer(id: document.Id, document.Type);
                  }
               }
            }
            catch (Exception e)
            {
               _logger.LogWarning("Failed To Update: {}, Because: {}", document.FullPath, e.Message);
               _logger.LogDebug("{}", e.ToString());

               break;
            }
         }

         _logger.LogInformation("Startup Update Finished.");
      }

      private void AddFile(string file)
      {
         using MediaFile _ = _mediaLibrary.InsertMedia(file);
      }

      private void UpdateFile(string file)
      {
         using MediaContainer _ = _mediaLibrary.UpdateMediaContainer(path: file);
      }

      public Task StopAsync(CancellationToken cancellationToken)
      {
         _watchers.Clear();

         _logger.LogInformation("Scanner Service Stopped.");

         return Task.CompletedTask;
      }

      private void OnError(object source, ErrorEventArgs e)
      {
         // Specify what is done when an error has occured.
         Debug.WriteLine($"Error: {e}");
      }

      private void OnCreated(object source, FileSystemEventArgs e)
      {
         // Specify what is done when a file is created.
         Debug.WriteLine($"File {e.ChangeType}: {e.FullPath}");

         // Queue the add operation.
         _taskQueue.QueueBackgroundTask(e.FullPath, cancellationToken =>
         {
            return Task.Run(() => AddFile(e.FullPath), cancellationToken);
         });
      }

      private void OnChanged(object source, FileSystemEventArgs e)
      {
         // Specify what is done when a file is created, or deleted.
         Debug.WriteLine($"File {e.ChangeType}: {e.FullPath}");

         // Queue the add operation.
         _taskQueue.QueueBackgroundTask(e.FullPath, cancellationToken =>
         {
            return Task.Run(() => UpdateFile(e.FullPath), cancellationToken);
         });
      }

      private void OnRenamed(object source, RenamedEventArgs e)
      {
         // Specify what is done when a file is renamed.
         Debug.WriteLine($"File Renamed: {e.OldFullPath} -> {e.FullPath}");

         // Queue the delete operation.
         _taskQueue.QueueBackgroundTask(e.OldFullPath, cancellationToken =>
         {
            return Task.Run(() => UpdateFile(e.OldFullPath), cancellationToken);
         });

         // Queue the add operation.
         _taskQueue.QueueBackgroundTask(e.FullPath, cancellationToken =>
         {
            return Task.Run(() => AddFile(e.FullPath), cancellationToken);
         });
      }
   }
}
