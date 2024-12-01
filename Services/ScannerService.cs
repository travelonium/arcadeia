using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using MediaCurator.Configuration;
using MediaCurator.Solr;
using SolrNet;

namespace MediaCurator.Services
{
   public class ScannerService(ILogger<ScannerService> logger,
                               IServiceProvider services,
                               IOptions<ScannerSettings> settings,
                               IBackgroundTaskQueue taskQueue,
                               IHostApplicationLifetime applicationLifetime,
                               IMediaLibrary mediaLibrary,
                               NotificationService notificationService) : IHostedService
   {
      private readonly IServiceProvider _services = services;

      private readonly ScannerSettings _settings = settings.Value;

      private readonly ILogger<ScannerService> _logger = logger;

      private readonly IMediaLibrary _mediaLibrary = mediaLibrary;

      private readonly NotificationService _notificationService = notificationService;

      private readonly Dictionary<string, FileSystemWatcher> _watchers = [];

      private readonly IBackgroundTaskQueue _taskQueue = taskQueue;

      private readonly CancellationToken _cancellationToken = applicationLifetime.ApplicationStopping;

      private Timer? _periodicScanTimer;

      /// <summary>
      /// The lists of folders to be scanned for changes that are mounted or exist.
      /// </summary>
      public List<string> AvailableFolders => _settings.Folders.Where(Directory.Exists).ToList();

      /// <summary>
      /// The lists of folders to be watched for changes that are mounted or exist.
      /// </summary>
      public List<string> AvailableWatchedFolders => _settings.WatchedFolders.Where(Directory.Exists).ToList();

      public Task StartAsync(CancellationToken cancellationToken)
      {
         foreach (string folder in _settings.WatchedFolders)
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
                  //           | NotifyFilters.DirectoryName
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

         // Add the folders being watched or scanned to the MediaLibrary.
         foreach (var folder in AvailableFolders.Concat(AvailableWatchedFolders).ToList())
         {
            try
            {
               // Add the folder itself to the MediaLibrary if it hasn't been added yet.
               using MediaFolder _ = _mediaLibrary.InsertMediaFolder(folder);
            }
            catch (Exception e)
            {
               _logger.LogWarning("Failed To Insert Folder: {}, Because: {}", folder, e.Message);
               _logger.LogDebug("{}", e.ToString());
            }
         }

         // Start the startup scanning task if necessary.
         if (_settings.StartupScan)
         {
            _logger.LogInformation("Starting Startup Scanning...");

            foreach (var folder in AvailableFolders.Concat(AvailableWatchedFolders).ToList())
            {
               // Queue the folder startup folder scan.
               _taskQueue.QueueBackgroundTask(folder, cancellationToken =>
               {
                  string uuid = System.Guid.NewGuid().ToString();
                  return Task.Run(() => ScanAsync(uuid, folder, "Startup", _cancellationToken), cancellationToken);
               });
            }
         }

         // Schedule the periodic scanning task if necessary.
         if (_settings.PeriodicScanIntervalMilliseconds > 0)
         {
            _logger.LogInformation("Configuring Periodic Scanning...");

            _periodicScanTimer = new Timer(state =>
            {
               foreach (var folder in AvailableFolders)
               {
                  // Queue the folder periodic scan.
                  _taskQueue.QueueBackgroundTask(folder, cancellationToken =>
                  {
                     string uuid = System.Guid.NewGuid().ToString();

                     return Task.Run(() => ScanAsync(uuid, folder, "Periodic", _cancellationToken), cancellationToken);
                  });
               }
            }, null, _settings.PeriodicScanIntervalMilliseconds, _settings.PeriodicScanIntervalMilliseconds);
         }

         // Start the startup update task if necessary.
         if (_settings.StartupUpdate)
         {
            // Queue the startup update task.
            _taskQueue.QueueBackgroundTask("Startup Update", cancellationToken =>
            {
               string uuid = System.Guid.NewGuid().ToString();
               return Task.Run(() => UpdateAsync(uuid, _cancellationToken), cancellationToken);
            });

            _logger.LogInformation("Startup Update Queued.");
         }

         // Start the background task processor.
         Task.Run(() => BackgroundTaskProcessorAsync(_cancellationToken), cancellationToken);

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

      private async Task ScanAsync(string uuid, string path, string type, CancellationToken cancellationToken)
      {
         var watch = new Stopwatch();
         var patterns = _settings.IgnoredPatterns.Select(pattern => new Regex(pattern, RegexOptions.IgnoreCase)).ToList();

         var fileSystemService = _services.GetService<IFileSystemService>();
         if ((fileSystemService != null) && fileSystemService.Mounts.Any(mount => path.StartsWith(mount.Folder) && !mount.Available))
         {
            _logger.LogWarning("{} Scanning Cancelled: {}", type, path);

            return;
         }

         _logger.LogInformation("{} Scanning Started: {}", type, path);

         watch.Start();

         try
         {
            _logger.LogInformation("Enumerating Files...");

            var files = new DirectoryInfo(path).GetFiles("*.*", SearchOption.AllDirectories)
                                               .AsParallel()
                                               .OrderBy(file => file.LastWriteTime)
                                               .Select(file => file.FullName)
                                               .ToList();
            int index = -1;
            int total = files.Count;

            _logger.LogInformation("Scanning {} Files...", total);

            // Use SemaphoreSlim to control concurrency for async tasks
            var semaphore = new SemaphoreSlim(_settings.ParallelScannerTasks);
            var tasks = files.Select(async file =>
            {
               await semaphore.WaitAsync(cancellationToken);

               try
               {
                  var name = Path.GetFileName(file);
                  int currentIndex = Interlocked.Increment(ref index);

                  // Inform the client(s) of the current progress
                  await _notificationService.ShowScanProgressAsync(uuid, $"{type} Scanning", path, file, currentIndex, total);

                  // Should the file name be ignored
                  foreach (Regex pattern in patterns)
                  {
                     if (pattern.IsMatch(file) || pattern.IsMatch(name))
                     {
                        _logger.LogDebug("File Ignored: {}", file);

                        return;
                     }
                  }

                  try
                  {
                     // Add the file to the MediaLibrary
                     using var newMediaFile = _mediaLibrary.InsertMediaFile(file);
                  }
                  catch (Exception e)
                  {
                     _logger.LogWarning("Failed To Insert: {}, Because: {}", file, e.Message);
                     _logger.LogDebug("{}", e.ToString());
                  }
               }
               finally
               {
                  semaphore.Release();
               }
            });

            // Wait for all tasks to complete
            await Task.WhenAll(tasks);

            _mediaLibrary.ClearCache();

            watch.Stop();
            var ts = watch.Elapsed;

            // Inform the client(s) of the need to refresh
            await _notificationService.RefreshAsync(path);

            _logger.LogInformation("{} Scanning Finished: {}", type, path);
            _logger.LogInformation("Took {} Days, {} Hours, {} Minutes, {} Seconds.",
                                   ts.Days, ts.Hours, ts.Minutes, ts.Seconds);
         }
         catch (DirectoryNotFoundException)
         {
            _logger.LogWarning("Path Unavailable: {}", path);

            return;
         }
         catch (UnauthorizedAccessException e)
         {
            _logger.LogWarning("{} Scanning Failed: {}, Because: {}", type, path, e.Message);

            return;
         }
         catch (Exception e)
         {
            _logger.LogError("Unexpected Error While {} Scanning: {}", type, e.Message);
            _logger.LogDebug("{}", e.ToString());

            return;
         }
      }

      private async Task UpdateAsync(string uuid, CancellationToken cancellationToken)
      {
         var watch = new Stopwatch();

         _logger.LogInformation("Startup Update Started.");

         watch.Start();

         // Consume the scoped Solr Index Service
         using var scope = _services.CreateScope();
         var solrIndexService = scope.ServiceProvider.GetRequiredService<ISolrIndexService<Models.MediaContainer>>();

         // Enumerate all the media files from the MediaLibrary database
         var documents = solrIndexService.Get(SolrQuery.All);
         var availableFolders = AvailableFolders.Concat(AvailableWatchedFolders).ToList();

         _logger.LogInformation("Calculating Documents Count...");

         int total = documents.Count;
         int index = -1;

         _logger.LogInformation("Updating {} Media Library Entries...", total);

         try
         {
            // Use SemaphoreSlim to control concurrency
            var semaphore = new SemaphoreSlim(_settings.ParallelScannerTasks);
            var tasks = documents.Select(async document =>
            {
               await semaphore.WaitAsync(cancellationToken);

               try
               {
                  int currentIndex = Interlocked.Increment(ref index);

                  if (string.IsNullOrEmpty(document.FullPath))
                  {
                     _logger.LogWarning("Invalid Media Container Detected: Id: {}, FullPath: {}", document.Id, document.FullPath);

                     return;
                  }

                  // Inform the client(s) of the current progress
                  await _notificationService.ShowUpdateProgressAsync(uuid, "Updating Media Library", document.FullPath, currentIndex, total);

                  try
                  {
                     if (!string.IsNullOrEmpty(document.Id) && !string.IsNullOrEmpty(document.FullPath))
                     {
                        // Find duplicates in Solr
                        var query = new SolrQueryByField("fullPath", document.FullPath);
                        var orders = new[]
                        {
                           new SortOrder("dateAdded", Order.ASC)
                        };

                        var results = solrIndexService.Get(query, orders);
                        if (results.Count > 1)
                        {
                           _logger.LogWarning("{} Duplicate Solr Entries Detected: {}: {}", results.Count, "fullPath", document.FullPath);

                           for (int i = 1; i < results.Count; i++)
                           {
                              var result = results[i];
                              if (solrIndexService.Delete(result))
                              {
                                 _logger.LogInformation("Duplicate {} Removed: {}", result.Type, result.Id);
                              }
                           }
                        }

                        // Ensure the path is in a watched and available folder
                        if (availableFolders.Any(folder => document.FullPath.StartsWith(folder)))
                        {
                           // Update the current media container
                           using var mediaContainer = _mediaLibrary.UpdateMediaContainer(id: document.Id, document.Type);
                        }
                     }
                  }
                  catch (Exception e)
                  {
                     _logger.LogWarning("Failed To Update: {}, Because: {}", document.FullPath, e.Message);
                     _logger.LogDebug("{}", e.ToString());
                  }
               }
               finally
               {
                  semaphore.Release();
               }
            });

            // Wait for all tasks to complete
            await Task.WhenAll(tasks);
         }
         catch (Exception e)
         {
            _logger.LogError("Unexpected Error While Updating: {}", e.Message);
            _logger.LogDebug("{}", e.ToString());
         }

         _mediaLibrary.ClearCache();

         watch.Stop();
         var ts = watch.Elapsed;

         // Inform the client(s) of the need to refresh
         await _notificationService.RefreshAsync("/");

         _logger.LogInformation("Startup Update Finished After {} Days, {} Hours, {} Minutes, {} Seconds.",
                                ts.Days, ts.Hours, ts.Minutes, ts.Seconds);
      }

      private void AddFile(string file)
      {
         using MediaFile? _ = _mediaLibrary.InsertMediaFile(file);
      }

      private void UpdateFile(string file)
      {
         using MediaContainer? _ = _mediaLibrary.UpdateMediaContainer(path: file);
      }

      public Task StopAsync(CancellationToken cancellationToken)
      {
         _watchers.Clear();

         _logger.LogInformation("Scanner Service Stopped.");

         return Task.CompletedTask;
      }

      private void OnError(object source, ErrorEventArgs e)
      {
         // Specify what is done when an error has occurred.
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
