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

      /// <summary>
      /// The lists of folders to be watched for changes.
      /// </summary>
      public List<string> WatchedFolders
      {
         get
         {
            return _configuration.GetSection("Scanner:WatchedFolders").Get<List<string>>();
         }
      }

      /// <summary>
      /// Determines whether or not the startup scanning is enabled.
      /// </summary>
      public bool StartupScan
      {
         get
         {
            return _configuration.GetSection("Scanner:StartupScan").Get<bool>();
         }
      }

      /// <summary>
      /// Determines whether or not the startup update is enabled.
      /// </summary>
      public bool StartupUpdate
      {
         get
         {
            return _configuration.GetSection("Scanner:StartupUpdate").Get<bool>();
         }
      }

      /// <summary>
      /// Determines whether or not the startup cleanup is enabled.
      /// </summary>
      public bool StartupCleanup
      {
         get
         {
            return _configuration.GetSection("Scanner:StartupCleanup").Get<bool>();
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

      [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
      public Task StartAsync(CancellationToken cancellationToken)
      {
         foreach (string folder in WatchedFolders)
         {
            if (!Directory.Exists(folder))
            {
               Debug.WriteLine("Watched Folder Unavailable: " + folder);

               continue;
            }

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

         // Calculate the total number of files to be processed. This will be used for the Total Progress.

         // Update the Status message.
         _progressStatus.Report("Calculating...");

         // Start the startup scanning task if necessary.
         if (StartupScan)
         {
            _totalCounter = 0.0;
            _totalCounterMax = 0.0;

            foreach (var folder in WatchedFolders)
            {
               try
               {
                  var files = Directory.EnumerateFiles(folder, "*.*", SearchOption.AllDirectories);
                  _totalCounterMax += files.Count();
               }
               catch (System.IO.DirectoryNotFoundException)
               {
                  // This is strange. A location previously specified seems to have been deleted. We can all but ignore it.
                  Debug.WriteLine("Location Not Found! : {0}", folder);
                  continue;
               }

               // Queue the folder startup folder scan.
               _taskQueue.QueueBackgroundWorkItem(folder, cancellationToken =>
               {
                  return Task.Run(() => Scan(folder, _totalCounterMax, ref _totalCounter, _progressFile, _progressFolder, _progressTotal, _progressThumbnailPreview, _progressStatus, _cancellationToken));
               });
            }
         }

         // Start the startup update task if necessary.
         if (StartupUpdate)
         {
            // Queue the startup update task.
            _taskQueue.QueueBackgroundWorkItem("Startup Update", cancellationToken =>
            {
               return Task.Run(() => Update(ref _totalCounterMax, ref _totalCounter, _progressFile, _progressTotal, _progressThumbnailPreview, _progressStatus, _cancellationToken));
            });
         }

         // Start the background task processor.
         Task.Run(() => BackgroundTaskProcessorAsync(_cancellationToken).Wait());

         _logger.LogInformation("Scanner Service Started.");

         return Task.CompletedTask;
      }

      private async Task BackgroundTaskProcessorAsync(CancellationToken cancellationToken)
      {
         _logger.LogInformation("Scanner Service Background Task Processor Started.");

         while (!cancellationToken.IsCancellationRequested)
         {
            var workItem = await _taskQueue.DequeueAsync(cancellationToken);

            try
            {
               await workItem(cancellationToken);
            }
            catch (Exception e)
            {
               _logger.LogError(e, "Error executing {WorkItem}.", nameof(workItem));
            }
         }

         _logger.LogInformation("Scanner Service Background Task Processor Stopped.");
      }

      private void Scan(string path,
                        double totalCounterMax,
                        ref double totalCounter,
                        IProgress<Tuple<double, double>> progressFile,
                        IProgress<Tuple<double, double>> progressFolder,
                        IProgress<Tuple<double, double>> progressTotal,
                        IProgress<byte[]> progressThumbnailPreview,
                        IProgress<string> progressStatus,
                        CancellationToken cancellationToken)
      {
         _logger.LogInformation("Startup Scanning Started: " + path);

         progressTotal.Report(new Tuple<double, double>(totalCounter, totalCounterMax));

         double fileCounter = 1.0;

         // Update the Status message.
         progressStatus.Report("Enumerating Files...");

         var files = Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories);

         // Calculate the total number of files in this location.
         double fileCounterMax = files.Count();

         // Initialize the Current Folder Progress.
         progressFolder.Report(new Tuple<double, double>(fileCounter, fileCounterMax));

         // Update the Status message.
         progressStatus.Report("Processing Files...");

         // Loop through the files in the specific MediaLocation.
         foreach (var file in files)
         {
            if (cancellationToken.IsCancellationRequested)
            {
               // Let's update the MediaLibrary with the new entries if any so far.
               _mediaLibrary.UpdateDatabase();

               // Return gracefully now!
               return;
            }

            // Update the Status message.
            progressStatus.Report(file);

            // Add the file to the MediaLibrary.                     
            MediaFile newMediaFile = _mediaLibrary.InsertMedia(file, progressFile, progressThumbnailPreview);

            // Update the Current Folder Progress.
            progressFolder.Report(new Tuple<double, double>(fileCounter++, fileCounterMax));

            // Update the Total Progress.
            progressTotal.Report(new Tuple<double, double>(totalCounter++, totalCounterMax));

            // Clear the thumbnail preview.
            progressThumbnailPreview.Report(null);
         }

         // Now that we're done with this location, let's update the MediaLibrary with the new entries if any.
         _mediaLibrary.UpdateDatabase();

         _logger.LogInformation("Startup Scanning Finished: " + path);
      }

      private void Update(ref double totalCounterMax,
                          ref double totalCounter,
                          IProgress<Tuple<double, double>> progressFile,
                          IProgress<Tuple<double, double>> progressTotal,
                          IProgress<byte[]> progressThumbnailPreview,
                          IProgress<string> progressStatus,
                          CancellationToken cancellationToken)
      {
         _logger.LogInformation("Startup Update Started.");

         // It's now time to go through the MediaLibrary itself and check for changes on the disk. 

         totalCounter = 0.0;
         progressStatus.Report("Calculating...");

         // Enumerate all the media files from the MediaLibrary database. This is used to detect
         // whether or not any files have been physically removed and thus update the MediaLibrary.
         IEnumerable<XElement> mediaFiles = from element in MediaLibrary.Document.Descendants()
                                            where ((element.Name == "Audio") ||
                                                   (element.Name == "Video") ||
                                                   (element.Name == "Photo"))
                                            select element;

         Debug.WriteLine("\r\nProcessing {0} Media Library Entries...", mediaFiles.Count());

         // Update the total number of files to be processed according to the number of media 
         // files currently present in the database.
         totalCounterMax = mediaFiles.Count();

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

            // Instantiate a MediaFile using the acquired element.
            MediaFile mediaFile = new MediaFile(_configuration, _thumbnailsDatabase, element);

            if (mediaFile != null)
            {
               // Update the Status message.
               progressStatus.Report(mediaFile.FullPath);

               // Update the current media file element.
               _mediaLibrary.UpdateMedia(element, progressFile, progressThumbnailPreview);
            }

            // Update the Total Progress.
            progressTotal.Report(new Tuple<double, double>(totalCounter++, totalCounterMax));

            // Clear the thumbnail preview.
            progressThumbnailPreview.Report(null);
         }

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

      private void OnCreated(object source, FileSystemEventArgs e)
      {
         // Specify what is done when a file is created.
         Debug.WriteLine($"File {e.ChangeType}: {e.FullPath}");

         // Queue the add operation.
         _taskQueue.QueueBackgroundWorkItem(e.FullPath, cancellationToken =>
         {
            return Task.Run(() => AddFile(e.FullPath));
         });
      }

      private void OnChanged(object source, FileSystemEventArgs e)
      {
         // Specify what is done when a file is created, or deleted.
         Debug.WriteLine($"File {e.ChangeType}: {e.FullPath}");

         // Queue the add operation.
         _taskQueue.QueueBackgroundWorkItem(e.FullPath, cancellationToken =>
         {
            return Task.Run(() => UpdateFile(e.FullPath));
         });
      }

      private void OnRenamed(object source, RenamedEventArgs e)
      {
         // Specify what is done when a file is renamed.
         Debug.WriteLine($"File Renamed: {e.OldFullPath} -> {e.FullPath}");

         // Queue the delete operation.
         _taskQueue.QueueBackgroundWorkItem(e.OldFullPath, cancellationToken =>
         {
            return Task.Run(() => UpdateFile(e.OldFullPath));
         });

         // Queue the add operation.
         _taskQueue.QueueBackgroundWorkItem(e.FullPath, cancellationToken =>
         {
            return Task.Run(() => AddFile(e.FullPath));
         });
      }
   }
}
