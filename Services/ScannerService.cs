using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Security.Permissions;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using System.Text.RegularExpressions;

namespace MediaCurator.Services
{
   public class ScannerService : IHostedService, IDisposable
   {
      protected readonly IConfiguration _configuration;

      private readonly ILogger<ScannerService> _logger;

      private readonly IThumbnailsDatabase _thumbnailsDatabase;

      private readonly IMediaLibrary _mediaLibrary;

      private readonly Dictionary<string, FileSystemWatcher> _watchers = new Dictionary<string, FileSystemWatcher>();

      private readonly IBackgroundTaskQueue _taskQueue;

      private readonly CancellationToken _cancellationToken;

      private double _totalCounter = 0.0;

      private double _totalCounterMax = 0.0;

      private readonly IProgress<Tuple<double, double>> _progressFile = new Progress<Tuple<double, double>>();

      private readonly IProgress<Tuple<double, double>> _progressFolder = new Progress<Tuple<double, double>>();

      private readonly IProgress<Tuple<double, double>> _progressTotal = new Progress<Tuple<double, double>>();

      private readonly IProgress<byte[]> _progressThumbnailPreview = new Progress<byte[]>();

      private readonly IProgress<string> _progressStatus = new Progress<string>();

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
      /// Determines the interval in milliseconds at which the database is updated while scanning in order to avoid losing the progress.
      /// </summary>
      public long DatabaseUpdateInterval
      {
         get
         {
            if (_configuration.GetSection("Scanner:DatabaseUpdateInterval").Exists())
            {
               return _configuration.GetSection("Scanner:DatabaseUpdateInterval").Get<long>();
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

      public ScannerService(IConfiguration configuration,
                            ILogger<ScannerService> logger,
                            IBackgroundTaskQueue taskQueue,
                            IHostApplicationLifetime applicationLifetime,
                            IThumbnailsDatabase thumbnailsDatabase,
                            IMediaLibrary mediaLibrary)
      {
         _logger = logger;
         _taskQueue = taskQueue;
         _mediaLibrary = mediaLibrary;
         _configuration = configuration;
         _thumbnailsDatabase = thumbnailsDatabase;
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
               _logger.LogWarning("Failed To Watch: {} Because: {}", folder, e.Message);
            }
         }

         // Calculate the total number of files to be processed. This will be used for the Total Progress.

         // Update the Status message.
         _progressStatus.Report("Calculating...");

         // Start the startup scanning task if necessary.
         if (StartupScan)
         {
            _logger.LogInformation("Starting Startup Scanning...");

            _totalCounter = 0.0;
            _totalCounterMax = 0.0;

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
               return Task.Run(() => Update(ref _totalCounterMax, ref _totalCounter, _progressFile, _progressTotal, _progressThumbnailPreview, _progressStatus, _cancellationToken), cancellationToken);
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
         Stopwatch watch = new();
         var interval = DatabaseUpdateInterval;
         var patterns = IgnoredFileNames.Select(pattern => new Regex(pattern, RegexOptions.IgnoreCase)).ToList<Regex>();

         _logger.LogInformation("{} Scanning Started: {}", type, path);

         try
         {
            _totalCounterMax += Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories).Count();
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
            _logger.LogWarning("Access Denied To: {} Because: {}", path, e.Message);

            return;
         }

         _progressTotal.Report(new Tuple<double, double>(_totalCounter, _totalCounterMax));

         double fileCounter = 1.0;

         // Update the Status message.
         _progressStatus.Report("Enumerating Files...");

         var files = Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories);

         // Calculate the total number of files in this location.
         double fileCounterMax = files.Count();

         // Initialize the Current Folder Progress.
         _progressFolder.Report(new Tuple<double, double>(fileCounter, fileCounterMax));

         // Update the Status message.
         _progressStatus.Report("Processing Files...");

         // Start the watch
         if (interval > 0) watch.Start();

         // Loop through the files in the specific MediaLocation.
         foreach (var file in files)
         {
            var name = Path.GetFileName(file);

            if (_cancellationToken.IsCancellationRequested)
            {
               // Let's update the MediaLibrary with the new entries if any so far.
               _mediaLibrary.UpdateDatabase();

               // Return gracefully now!
               return;
            }

            // Should the file name be ignored
            foreach (Regex pattern in patterns)
            {
               if (pattern.IsMatch(name))
               {
                  _logger.LogDebug("IGNORED: {}", file);

                  goto Skip;
               }
            }

            // Update the Status message.
            _progressStatus.Report(file);

            try
            {
               // Add the file to the MediaLibrary.
               MediaFile newMediaFile = _mediaLibrary.InsertMedia(file, _progressFile, _progressThumbnailPreview);
            }
            catch (Exception e)
            {
               _logger.LogError("Failed To Insert: {} Because: {}", file, e.Message);

               break;
            }

            // Update the MediaLibrary if the interval has ellapsed.
            if ((interval > 0) && (watch.ElapsedMilliseconds > interval))
            {
               _mediaLibrary.UpdateDatabase();

               watch.Restart();
            }

         Skip:

            // Update the Current Folder Progress.
            _progressFolder.Report(new Tuple<double, double>(fileCounter++, fileCounterMax));

            // Update the Total Progress.
            _progressTotal.Report(new Tuple<double, double>(_totalCounter++, _totalCounterMax));

            // Clear the thumbnail preview.
            _progressThumbnailPreview.Report(null);
         }

         // Now that we're done with this location, let's update the MediaLibrary with the new entries if any.
         _mediaLibrary.UpdateDatabase();

         _logger.LogInformation("{} Scanning Finished: {}", type, path);
      }

      private void Update(ref double totalCounterMax,
                          ref double totalCounter,
                          IProgress<Tuple<double, double>> progressFile,
                          IProgress<Tuple<double, double>> progressTotal,
                          IProgress<byte[]> progressThumbnailPreview,
                          IProgress<string> progressStatus,
                          CancellationToken cancellationToken)
      {
         Stopwatch watch = new();
         var interval = DatabaseUpdateInterval;

         _logger.LogInformation("Startup Update Started.");

         // It's now time to go through the MediaLibrary itself and check for changes on the disk. 

         totalCounter = 0.0;
         progressStatus.Report("Calculating...");

         // Enumerate all the media files from the MediaLibrary database. This is used to detect
         // whether or not any files have been physically removed and thus update the MediaLibrary.
         IEnumerable<XElement> mediaFiles = from element in _mediaLibrary.Self.Descendants()
                                            where ((element.Name == "Audio") ||
                                                   (element.Name == "Video") ||
                                                   (element.Name == "Photo"))
                                            select element;

         Debug.WriteLine("\r\nProcessing {0} Media Library Entries...", mediaFiles.Count());

         // Update the total number of files to be processed according to the number of media 
         // files currently present in the database.
         totalCounterMax = mediaFiles.Count();

         // Start the watch
         if (interval > 0) watch.Start();

         // Loop through the mediaFiles.
         foreach (XElement element in mediaFiles)
         {
            if (cancellationToken.IsCancellationRequested)
            {
               // Let's update the MediaLibrary with the new entries if any so far.
               _mediaLibrary.UpdateDatabase();

               // Return gracefully now!
               return;
            }

            try
            {
               // Instantiate a MediaFile using the acquired element.
               MediaFile mediaFile = new MediaFile(_configuration, _thumbnailsDatabase, _mediaLibrary, element);

               if (mediaFile != null)
               {
                  // Update the Status message.
                  progressStatus.Report(mediaFile.FullPath);

                  // Make sure if the path is located in a watched folder, that folder is available.
                  if ((WatchedFolders.Count(folder => mediaFile.FullPath.StartsWith(folder)) == 0) ||
                      (AvailableWatchedFolders.Count(folder => mediaFile.FullPath.StartsWith(folder)) > 0))
                  {
                     // Update the current media file element.
                     _mediaLibrary.UpdateMedia(element, progressFile, progressThumbnailPreview);
                  }
               }
            }
            catch (Exception e)
            {
               _logger.LogError("An exception has been thrown while updating the Media Library: {}", e.Message);

               break;
            }

            // Update the MediaLibrary if the interval has ellapsed.
            if ((interval > 0) && (watch.ElapsedMilliseconds > interval))
            {
               _mediaLibrary.UpdateDatabase();

               watch.Restart();
            }

            // Update the Total Progress.
            progressTotal.Report(new Tuple<double, double>(totalCounter++, totalCounterMax));

            // Clear the thumbnail preview.
            progressThumbnailPreview.Report(null);
         }

         // Now that we're done with this task, let's update the MediaLibrary with the new changes.
         _mediaLibrary.UpdateDatabase();

         _logger.LogInformation("Startup Update Finished.");
      }

      private void AddFile(string file)
      {
         var progressFile = new Progress<Tuple<double, double>>();
         var progressThumbnailPreview = new Progress<byte[]>();

         MediaFile mediaFile = _mediaLibrary.InsertMedia(file, progressFile, progressThumbnailPreview);

         _mediaLibrary.UpdateDatabase();
      }

      private void UpdateFile(string file)
      {
         var progressFile = new Progress<Tuple<double, double>>();
         var progressThumbnailPreview = new Progress<byte[]>();

         MediaFile mediaFile = _mediaLibrary.UpdateMedia(file, progressFile, progressThumbnailPreview);

         _mediaLibrary.UpdateDatabase();
      }

      public Task StopAsync(CancellationToken cancellationToken)
      {
         _watchers.Clear();

         _logger.LogInformation("Scanner Service Stopped.");

         return Task.CompletedTask;
      }

      public void Dispose()
      {
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
