using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using MediaCurator.Configuration;
using MediaCurator.Solr;
using SolrNet;

namespace MediaCurator.Services
{
   public class ScannerService(IServiceProvider services,
                               IMediaLibrary mediaLibrary,
                               ILogger<ScannerService> logger,
                               IBackgroundTaskQueue taskQueue,
                               NotificationService notificationService,
                               IOptionsMonitor<ScannerSettings> settings,
                               IHostApplicationLifetime applicationLifetime) : IScannerService
   {
      private Timer? _periodicScanTimer;
      private static Task? _backgroundTaskProcessor;
      private readonly SemaphoreSlim _semaphore = new(1, 1);
      private readonly IServiceProvider _services = services;
      private readonly ILogger<ScannerService> _logger = logger;
      private readonly IMediaLibrary _mediaLibrary = mediaLibrary;
      private readonly IBackgroundTaskQueue _taskQueue = taskQueue;
      private CancellationTokenSource _cancellationTokenSource = new();
      private readonly Dictionary<string, FileSystemWatcher> _watchers = [];
      private readonly IOptionsMonitor<ScannerSettings> _settings = settings;
      private readonly NotificationService _notificationService = notificationService;
      private readonly IHostApplicationLifetime _applicationLifetime = applicationLifetime;

      public bool Updating
      {
         get;
         set;
      } = false;

      public bool Scanning
      {
         get;
         set;
      } = false;

      /// <summary>
      /// The lists of folders to be scanned for changes that are mounted or exist.
      /// </summary>
      public List<string> AvailableFolders => _settings.CurrentValue.Folders.Where(Directory.Exists).ToList();

      /// <summary>
      /// The lists of folders to be watched for changes that are mounted or exist.
      /// </summary>
      public List<string> AvailableWatchedFolders => _settings.CurrentValue.WatchedFolders.Where(Directory.Exists).ToList();

      public List<FileSystemMount> Mounts => throw new NotImplementedException();

      public Task StartAsync(CancellationToken cancellationToken)
      {
         _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_applicationLifetime.ApplicationStopping, cancellationToken);

         _logger.LogInformation("Starting Scanner Service...");

         foreach (string folder in _settings.CurrentValue.WatchedFolders)
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
         if (_settings.CurrentValue.StartupScan)
         {
            _logger.LogInformation("Starting Startup Scanning...");

            foreach (var folder in AvailableFolders.Concat(AvailableWatchedFolders).ToList())
            {
               // Queue the folder startup folder scan.
               _taskQueue.Queue(folder, cancellationToken =>
               {
                  string uuid = System.Guid.NewGuid().ToString();
                  return Task.Run(() => ScanAsync(uuid, folder, "Startup", _cancellationTokenSource.Token), _cancellationTokenSource.Token);
               });
            }
         }

         // Schedule the periodic scanning task if necessary.
         if (_settings.CurrentValue.PeriodicScanIntervalMilliseconds > 0)
         {
            _logger.LogInformation("Configuring Periodic Scanning...");

            _periodicScanTimer = new Timer(state =>
            {
               foreach (var folder in AvailableFolders)
               {
                  // Queue the folder periodic scan.
                  _taskQueue.Queue(folder, cancellationToken =>
                  {
                     string uuid = System.Guid.NewGuid().ToString();

                     return Task.Run(() => ScanAsync(uuid, folder, "Periodic", _cancellationTokenSource.Token), _cancellationTokenSource.Token);
                  });
               }
            }, null, _settings.CurrentValue.PeriodicScanIntervalMilliseconds, _settings.CurrentValue.PeriodicScanIntervalMilliseconds);
         }

         // Start the startup update task if necessary.
         if (_settings.CurrentValue.StartupUpdate)
         {
            // Queue the startup update task.
            _taskQueue.Queue("Startup Update", cancellationToken =>
            {
               string uuid = System.Guid.NewGuid().ToString();

               return Task.Run(() => UpdateAsync(uuid, _cancellationTokenSource.Token), _cancellationTokenSource.Token);
            });

            _logger.LogInformation("Startup Update Queued.");
         }

         // Start the background task processor.
         _backgroundTaskProcessor = Task.Run(async () => await BackgroundTaskProcessorAsync(_cancellationTokenSource.Token), _cancellationTokenSource.Token);

         _logger.LogInformation("Scanner Service Started.");

         return Task.CompletedTask;
      }

      public async Task StopAsync(CancellationToken cancellationToken)
      {
         _logger.LogInformation("Stopping Scanner Service...");

         _cancellationTokenSource.Cancel();

         if (_backgroundTaskProcessor == null)
         {
            throw new Exception("Background Task Processor Not Initialized.");
         }

         await _backgroundTaskProcessor.WaitAsync(cancellationToken);

         _watchers.Clear();
         _taskQueue.Clear();
         _periodicScanTimer?.Dispose();

         _logger.LogInformation("Scanner Service Stopped.");
      }

      private async Task BackgroundTaskProcessorAsync(CancellationToken cancellationToken)
      {
         _logger.LogInformation("Scanner Service Background Task Processor Started.");

         while (!cancellationToken.IsCancellationRequested)
         {
            try
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
            catch (OperationCanceledException)
            {
               break;
            }
         }

         _logger.LogInformation("Scanner Service Background Task Processor Stopped.");
      }

      public async Task ScanAsync(string uuid, string path, string type, CancellationToken cancellationToken)
      {
         Scanning = true;
         var watch = new Stopwatch();
         var patterns = _settings.CurrentValue.IgnoredPatterns.Select(pattern => new Regex(pattern, RegexOptions.IgnoreCase)).ToList();

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

            var files = new DirectoryInfo(path).EnumerateFiles("*.*", SearchOption.AllDirectories)
                                               .AsParallel()
                                               .WithCancellation(cancellationToken)
                                               .OrderBy(file =>
                                               {
                                                  try
                                                  {
                                                     return file.LastWriteTime;
                                                  }
                                                  catch (ArgumentOutOfRangeException)
                                                  {
                                                     return DateTime.MinValue;
                                                  }
                                               })
                                               .Select(file => file.FullName)
                                               .ToList();

            _logger.LogTrace("Counting Files...");

            int index = -1;
            int total = files.Count;

            _logger.LogInformation("Scanning {} Files...", total);

            // Use SemaphoreSlim to control concurrency for async tasks
            var semaphore = new SemaphoreSlim(_settings.CurrentValue.ParallelScannerTasks);
            var tasks = new List<Task>();

            foreach (var file in files)
            {
               cancellationToken.ThrowIfCancellationRequested();

               await semaphore.WaitAsync(cancellationToken);

               var task = Task.Run(async () =>
               {
                  try
                  {
                     var name = Path.GetFileName(file);
                     int currentIndex = Interlocked.Increment(ref index);

                     // Inform the client(s) of the current progress
                     await _notificationService.ShowScanProgressAsync(uuid, $"{type} Scanning", path, file, currentIndex, total);

                     cancellationToken.ThrowIfCancellationRequested();

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
               }, CancellationToken.None);

               tasks.Add(task);
            }

            // Wait for all tasks to complete
            await Task.WhenAll(tasks);

            _mediaLibrary.ClearCache();

            watch.Stop();

            // Inform the client(s) of the need to refresh
            await _notificationService.RefreshAsync(path);
         }
         catch (OperationCanceledException)
         {
            _logger.LogInformation("{} Scanning Was Cancelled.", type);

            // Inform the client(s) of the cancellation
            await _notificationService.ScanCancelledAsync(uuid, String.Format("{0} Scanning Cancelled", type), path);
         }
         catch (DirectoryNotFoundException)
         {
            _logger.LogWarning("Path Unavailable: {}", path);
         }
         catch (UnauthorizedAccessException e)
         {
            _logger.LogWarning("{} Scanning Failed: {}, Because: {}", type, path, e.Message);
         }
         catch (Exception e)
         {
            _logger.LogError("Unexpected Error While {} Scanning: {}", type, e.Message);
            _logger.LogDebug("{}", e.ToString());
         }
         finally
         {
            watch.Stop();
         }

         Scanning = false;
         var ts = watch.Elapsed;
         _logger.LogInformation("{} Scanning {}: {}", type, path, cancellationToken.IsCancellationRequested ? "Cancelled" : "Finished");
         _logger.LogInformation("Took {} Days, {} Hours, {} Minutes, {} Seconds.", ts.Days, ts.Hours, ts.Minutes, ts.Seconds);
      }

      public async Task RestartAsync(CancellationToken cancellationToken)
      {
         _logger.LogInformation("Restarting Scanner Service...");

         // Ensure only one restart happens at a time.
         await _semaphore.WaitAsync(cancellationToken);

         try
         {
            // Stop the service
            await StopAsync(cancellationToken);

            // Start the service
            await StartAsync(cancellationToken);

            _logger.LogInformation("Scanner Service Restarted.");
         }
         catch (Exception ex)
         {
            _logger.LogError("Failed To Restart Scanner Service: {}", ex.Message);
         }
         finally
         {
            _semaphore.Release();
         }
      }

      public async Task UpdateAsync(string uuid, CancellationToken cancellationToken)
      {
         Updating = true;
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
            var semaphore = new SemaphoreSlim(_settings.CurrentValue.ParallelScannerTasks);
            var tasks = new List<Task>();

            foreach (var document in documents)
            {
               cancellationToken.ThrowIfCancellationRequested();

               await semaphore.WaitAsync(cancellationToken);

               var task = Task.Run(async () =>
               {
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

                     cancellationToken.ThrowIfCancellationRequested();

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
                              cancellationToken.ThrowIfCancellationRequested();

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
               }, CancellationToken.None);

               tasks.Add(task);
            }

            // Wait for all tasks to complete
            await Task.WhenAll(tasks);

            cancellationToken.ThrowIfCancellationRequested();
         }
         catch (OperationCanceledException)
         {
            _logger.LogInformation("Update Was Cancelled.");

            // Inform the client(s) of the cancellation
            await _notificationService.UpdateCancelledAsync(uuid, "Media Library Update Cancelled");
         }
         catch (Exception e)
         {
            _logger.LogError("Unexpected Error While Updating: {}", e.Message);
            _logger.LogDebug("{}", e.ToString());
         }

         _mediaLibrary.ClearCache();

         watch.Stop();

         // Inform the client(s) of the need to refresh
         await _notificationService.RefreshAsync("/");

         Updating = false;
         var ts = watch.Elapsed;
         _logger.LogInformation("Startup Update {} After {} Days, {} Hours, {} Minutes, {} Seconds.",
                                cancellationToken.IsCancellationRequested ? "Cancelled" : "Finished",
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
         _taskQueue.Queue(e.FullPath, cancellationToken =>
         {
            var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenSource.Token, cancellationToken);
            return Task.Run(() => AddFile(e.FullPath), linkedCancellationTokenSource.Token);
         });
      }

      private void OnChanged(object source, FileSystemEventArgs e)
      {
         // Specify what is done when a file is created, or deleted.
         Debug.WriteLine($"File {e.ChangeType}: {e.FullPath}");

         // Queue the add operation.
         _taskQueue.Queue(e.FullPath, cancellationToken =>
         {
            var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenSource.Token, cancellationToken);
            return Task.Run(() => UpdateFile(e.FullPath), linkedCancellationTokenSource.Token);
         });
      }

      private void OnRenamed(object source, RenamedEventArgs e)
      {
         // Specify what is done when a file is renamed.
         Debug.WriteLine($"File Renamed: {e.OldFullPath} -> {e.FullPath}");

         // Queue the delete operation.
         _taskQueue.Queue(e.OldFullPath, cancellationToken =>
         {
            var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenSource.Token, cancellationToken);
            return Task.Run(() => UpdateFile(e.OldFullPath), linkedCancellationTokenSource.Token);
         });

         // Queue the add operation.
         _taskQueue.Queue(e.FullPath, cancellationToken =>
         {
            var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenSource.Token, cancellationToken);
            return Task.Run(() => AddFile(e.FullPath), linkedCancellationTokenSource.Token);
         });
      }
   }
}
