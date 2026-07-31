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

using Microsoft.Extensions.Options;
using Arcadeia.Configuration;
using Arcadeia.Solr;

namespace Arcadeia.Services
{
   /// <summary>
   /// Transcribes video files in the background using whisper.cpp, decoupled from the
   /// ScannerService's per-file scan pipeline so that CPU-bound transcription never blocks or slows
   /// down scanning. Embedded subtitle streams are handled separately, inline during scanning - see
   /// VideoFile.DetectSubtitles() - since they're cheap and don't need whisper at all.
   /// </summary>
   public class TranscriptionService(IServiceProvider services,
                                     IMediaLibrary mediaLibrary,
                                     ILogger<TranscriptionService> logger,
                                     ITranscriptionQueue queue,
                                     IOptionsMonitor<Settings> settings,
                                     IHostApplicationLifetime applicationLifetime) : ITranscriptionService
   {
      private readonly List<Task> _workers = [];
      private readonly IServiceProvider _services = services;
      private readonly IMediaLibrary _mediaLibrary = mediaLibrary;
      private readonly ILogger<TranscriptionService> _logger = logger;
      private readonly ITranscriptionQueue _queue = queue;
      private readonly IOptionsMonitor<Settings> _settings = settings;
      private readonly IHostApplicationLifetime _applicationLifetime = applicationLifetime;

      // Serializes StartWorkers()/StopWorkersAsync() transitions, since both the initial StartAsync()
      // call and every subsequent live settings change funnel through ApplyEnabledStateAsync().
      private readonly SemaphoreSlim _transitionLock = new(1, 1);

      private CancellationTokenSource? _cancellationTokenSource;
      private IDisposable? _onChange;
      private bool _running;

      public async Task StartAsync(CancellationToken cancellationToken)
      {
         // React to Transcription.Enabled being toggled live via the Settings API, rather than only
         // consulting it once here - otherwise enabling transcription after having started with it
         // disabled would silently do nothing, since the worker pool would never get created.
         _onChange = _settings.OnChange((settings, name) => { _ = ApplyEnabledStateSafelyAsync(); });

         await ApplyEnabledStateAsync();

         if (!_running)
         {
            _logger.LogInformation("Transcription Service Disabled.");
         }
      }

      public async Task StopAsync(CancellationToken cancellationToken)
      {
         _onChange?.Dispose();

         await StopWorkersAsync(cancellationToken);
      }

      private async Task ApplyEnabledStateSafelyAsync()
      {
         try
         {
            await ApplyEnabledStateAsync();
         }
         catch (Exception e)
         {
            _logger.LogWarning("Failed To Apply Transcription Settings Change, Because: {}", e.Message);
         }
      }

      private async Task ApplyEnabledStateAsync()
      {
         await _transitionLock.WaitAsync();

         try
         {
            bool enabled = _settings.CurrentValue.Transcription.Enabled;

            if (enabled && !_running)
            {
               StartWorkers();
            }
            else if (!enabled && _running)
            {
               await StopWorkersAsync();
            }
         }
         finally
         {
            _transitionLock.Release();
         }
      }

      private void StartWorkers()
      {
         _logger.LogInformation("Starting Transcription Service...");

         _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_applicationLifetime.ApplicationStopping);
         _workers.Clear();

         int parallelTasks = Math.Max(1, _settings.CurrentValue.Transcription.ParallelTasks);

         for (int i = 0; i < parallelTasks; i++)
         {
            _workers.Add(Task.Run(() => WorkerAsync(_cancellationTokenSource.Token), _cancellationTokenSource.Token));
         }

         if (_settings.CurrentValue.Transcription.CatchUpOnStartup)
         {
            _workers.Add(Task.Run(() => CatchUpAsync(_cancellationTokenSource.Token), _cancellationTokenSource.Token));
         }

         _running = true;

         _logger.LogInformation("Transcription Service Started.");
      }

      private async Task StopWorkersAsync(CancellationToken cancellationToken = default)
      {
         if (!_running) return;

         _logger.LogInformation("Stopping Transcription Service...");

         _cancellationTokenSource?.Cancel();

         try
         {
            await Task.WhenAll(_workers).WaitAsync(cancellationToken);
         }
         catch (OperationCanceledException)
         {
         }

         _running = false;

         _logger.LogInformation("Transcription Service Stopped.");
      }

      private async Task WorkerAsync(CancellationToken cancellationToken)
      {
         try
         {
            await foreach (var id in _queue.DequeueAllAsync(cancellationToken))
            {
               try
               {
                  _logger.LogDebug("Transcribing: {}", id);

                  using var container = _mediaLibrary.UpdateMediaContainer(id: id, type: "Video");

                  if (container is VideoFile video)
                  {
                     video.GenerateTranscript(force: true);
                  }
               }
               catch (Exception e)
               {
                  _logger.LogWarning("Failed To Transcribe: {}, Because: {}", id, e.Message);
               }
               finally
               {
                  _queue.Complete(id);
               }
            }
         }
         catch (OperationCanceledException)
         {
         }
      }

      /// <summary>
      /// Queues every video still missing a transcript. This both survives restarts (the in-memory
      /// queue doesn't persist across them) and acts as the retry mechanism for videos whose previous
      /// transcription attempt crashed or timed out, since "still missing a transcript" is the same
      /// condition either way.
      /// </summary>
      private async Task CatchUpAsync(CancellationToken cancellationToken)
      {
         try
         {
            using IServiceScope scope = _services.CreateScope();
            ISolrIndexService<Models.MediaContainer> solrIndexService = scope.ServiceProvider.GetRequiredService<ISolrIndexService<Models.MediaContainer>>();

            var documents = solrIndexService.Get("type:Video AND -transcript:*");

            _logger.LogInformation("Queuing {} Video(s) Missing A Transcript...", documents.Count);

            foreach (var document in documents)
            {
               cancellationToken.ThrowIfCancellationRequested();

               if (!string.IsNullOrEmpty(document.Id))
               {
                  _queue.Enqueue(document.Id);
               }
            }

            await Task.CompletedTask;
         }
         catch (OperationCanceledException)
         {
         }
         catch (Exception e)
         {
            _logger.LogWarning("Failed To Queue Missing Transcripts, Because: {}", e.Message);
         }
      }
   }
}
