using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.SignalR;
using System.Reflection.Metadata;
using Newtonsoft.Json.Linq;
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

      private readonly NotificationService _notificationService;

      private readonly Dictionary<string, FileSystemWatcher> _watchers = new();

      private readonly IBackgroundTaskQueue _taskQueue;


      private readonly CancellationToken _cancellationToken;

      private Timer _periodicScanTimer;

      /// <summary>
      /// The lists of folders to be scanned for changes.
      /// </summary>
      public List<string> Folders
      {
         get
         {
            if (_configuration.GetSection("Scanner:Folders").Exists())
            {
               return _configuration.GetSection("Scanner:Folders").Get<List<string>>();
            }
            else
            {
               return new List<string>();
            }
         }
      }

      /// <summary>
      /// The lists of folders to be scanned for changes that are mounted or exist.
      /// </summary>
      public List<string> AvailableFolders
      {
         get
         {
            return Folders.Where(folder => Directory.Exists(folder)).ToList<string>();
         }
      }

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
      /// A list of regex patterns specifying which file or folder names to ignore when scanning.
      /// </summary>
      public List<String> IgnoredPatterns
      {
         get
         {
            if (_configuration.GetSection("Scanner:IgnoredPatterns").Exists())
            {
               return _configuration.GetSection("Scanner:IgnoredPatterns").Get<List<String>>();
            }
            else
            {
               return new List<String>();
            }
         }
      }

      /// <summary>
      /// The number of parallel scanner tasks while startup scanning.
      /// </summary>
      public int ParallelScannerTasks
      {
         get
         {
            if (_configuration.GetSection("Scanner:ParallelScannerTasks").Exists())
            {
               return _configuration.GetSection("Scanner:ParallelScannerTasks").Get<int>();
            }
            else
            {
               return -1;
            }
         }
      }

      public ScannerService(ILogger<ScannerService> logger,
                            IServiceProvider services,
                            IConfiguration configuration,
                            IBackgroundTaskQueue taskQueue,
                            IHostApplicationLifetime applicationLifetime,
                            IMediaLibrary mediaLibrary,
                            NotificationService notificationService)
      {
         _logger = logger;
         _services = services;
         _taskQueue = taskQueue;
         _mediaLibrary = mediaLibrary;
         _configuration = configuration;
         _notificationService = notificationService;
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
         if (StartupScan)
         {
            _logger.LogInformation("Starting Startup Scanning...");

            foreach (var folder in AvailableFolders.Concat(AvailableWatchedFolders).ToList())
            {
               // Queue the folder startup folder scan.
               _taskQueue.QueueBackgroundTask(folder, cancellationToken =>
               {
                  string uuid = System.Guid.NewGuid().ToString();
                  return Task.Run(() => Scan(uuid, folder, "Startup", _cancellationToken), cancellationToken);
               });
            }
         }

         // Schedule the periodic scanning task if necessary.
         if (PeriodicScanInterval > 0)
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
                     return Task.Run(() => Scan(uuid, folder, "Periodic", _cancellationToken), cancellationToken);
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
               string uuid = System.Guid.NewGuid().ToString();
               return Task.Run(() => Update(uuid, _cancellationToken), cancellationToken);
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

      private void Scan(string uuid, string path, string type, CancellationToken cancellationToken)
      {
         var watch = new Stopwatch();
         var patterns = IgnoredPatterns.Select(pattern => new Regex(pattern, RegexOptions.IgnoreCase)).ToList<Regex>();

         var fileSystemService = _services.GetService<IFileSystemService>();
         if ((fileSystemService != null) && fileSystemService.Mounts.Any(mount => (path.StartsWith(mount.Folder) && !mount.Available)))
         {
            _logger.LogWarning("{} Scanning Cancelled: {}", type, path);

            return;
         }

         _logger.LogInformation("{} Scanning Started: {}", type, path);

         watch.Start();

         try
         {
            var files = Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories);

            _logger.LogInformation("Calculating Files Count...");

            int total = files.Count();
            int index = -1;

            _logger.LogInformation("Scanning {} Files...", total);

            // Loop through the files in the specific MediaLocation in parallel.
            try
            {
               Parallel.ForEach(files, new ParallelOptions { MaxDegreeOfParallelism = ParallelScannerTasks }, async (file) =>
               {
                  var name = Path.GetFileName(file);
                  int currentIndex = Interlocked.Increment(ref index);

                  // Inform the client(s) of the current progress.
                  await _notificationService.ShowScanProgressAsync(uuid, String.Format("{0} Scanning", type), path, file, currentIndex, total);

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
                     // Add the file to the MediaLibrary.
                     using MediaFile newMediaFile = _mediaLibrary.InsertMediaFile(file);
                  }
                  catch (Exception e)
                  {
                     _logger.LogWarning("Failed To Insert: {}, Because: {}", file, e.Message);
                     _logger.LogDebug("{}", e.ToString());
                  }
               });
            }
            catch (AggregateException ae)
            {
               _logger.LogError("Encountered {} Exception(s):", ae.Flatten().InnerExceptions.Count);

               foreach (var e in ae.Flatten().InnerExceptions)
               {
                  _logger.LogDebug("{}", e.ToString());
               }
            }

            _mediaLibrary.ClearCache();

            watch.Stop();
            TimeSpan ts = new();
            ts = watch.Elapsed;

            _logger.LogInformation("{} Scanning Finished: {}", type, path);
            _logger.LogInformation("Took {} Days, {} Hours, {} Minutes, {} Seconds.",
                                   ts.Days, ts.Hours, ts.Minutes, ts.Seconds);
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

      private void Update(string uuid, CancellationToken cancellationToken)
      {
         var watch = new Stopwatch();

         _logger.LogInformation("Startup Update Started.");

         // It's now time to go through the MediaLibrary itself and check for changes on the disk.

         watch.Start();

         // Consume the scoped Solr Index Service.
         using IServiceScope scope = _services.CreateScope();
         ISolrIndexService<Models.MediaContainer> solrIndexService = scope.ServiceProvider.GetRequiredService<ISolrIndexService<Models.MediaContainer>>();

         // Enumerate all the media files from the MediaLibrary database. This is used to detect
         // whether or not any files have been physically removed and thus update the MediaLibrary.

         var documents = solrIndexService.Get(SolrQuery.All);
         var availableFolders = AvailableFolders.Concat(AvailableWatchedFolders).ToList();

         _logger.LogInformation("Calculating Documents Count...");

         int total = documents.Count;
         int index = -1;

         _logger.LogInformation("Updating {} Media Library Entries...", total);

         try
         {
            // Loop through the documents in the index in parallel.
            Parallel.ForEach(documents, new ParallelOptions { MaxDegreeOfParallelism = ParallelScannerTasks }, async (document) =>
            {
               int currentIndex = Interlocked.Increment(ref index);

               // Inform the client(s) of the current progress.
               await _notificationService.ShowUpdateProgressAsync(uuid, "Updating Media Library", document.FullPath, currentIndex, total);

               try
               {
                  if (!String.IsNullOrEmpty(document.Id) && !String.IsNullOrEmpty(document.FullPath))
                  {
                     // Find any possible duplicate entries with the same FullPath and remove them.
                     SolrQueryByField query = new("fullPath", document.FullPath);
                     SortOrder[] orders =
                     [
                        new SortOrder("dateAdded", Order.ASC)
                     ];

                     SolrQueryResults<Models.MediaContainer> results = solrIndexService.Get(query, orders);

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

                     // Make sure the path is located in a watched folder and that folder is available.
                     if (availableFolders.Any(folder => document.FullPath.StartsWith(folder)))
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
               }
            });
         }
         catch (AggregateException ae)
         {
            _logger.LogError("Encountered {} Exception(s):", ae.Flatten().InnerExceptions.Count);

            foreach (var e in ae.Flatten().InnerExceptions)
            {
               _logger.LogDebug("{}", e.ToString());
            }
         }

         _mediaLibrary.ClearCache();

         watch.Stop();
         TimeSpan ts = new();
         ts = watch.Elapsed;

         _logger.LogInformation("Startup Update Finished After {} Days, {} Hours, {} Minutes, {} Seconds.",
                                ts.Days, ts.Hours, ts.Minutes, ts.Seconds);
      }

      private void AddFile(string file)
      {
         using MediaFile _ = _mediaLibrary.InsertMediaFile(file);
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
