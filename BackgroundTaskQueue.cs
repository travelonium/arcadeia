using System.Collections.Concurrent;

namespace MediaCurator
{
   public class BackgroundTaskQueue(ILogger<BackgroundTaskQueue> logger) : IBackgroundTaskQueue
   {
      private readonly SemaphoreSlim _signal = new(0);
      private readonly ILogger<BackgroundTaskQueue> _logger = logger;
      private readonly ConcurrentDictionary<string, Timer> _timers = new();
      private readonly ConcurrentQueue<Tuple<string, Func<CancellationToken, Task>>> _tasks = new();

      public void Queue(string key, Func<CancellationToken, Task> task)
      {
         ArgumentNullException.ThrowIfNull(task, nameof(task));

         // Ensure the task is not already queued
         foreach (var entry in _tasks)
         {
            if (entry.Item1 == key)
            {
               return;
            }
         }

         if (_timers.TryGetValue(key, out var timer))
         {
            timer.Dispose();
            _timers.Remove(key, out _);
         }

         _timers.TryAdd(key, new Timer((_) =>
         {
            _tasks.Enqueue(new Tuple<string, Func<CancellationToken, Task>>(key, task));

            _logger.LogInformation("Queued: {}", key);

            _signal.Release();

         }, null, 15000, Timeout.Infinite));
      }

      public async Task<Func<CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken)
      {
         // Wait for a signal while respecting the caller's cancellation token
         await _signal.WaitAsync(cancellationToken);

         if (_tasks.TryDequeue(out var task))
         {
            _logger.LogInformation("Dequeued: {TaskName}", task.Item1);

            return task.Item2;
         }
         else
         {
            _logger.LogWarning("Dequeue Failed: No Tasks Available.");

            throw new InvalidOperationException("Failed to dequeue a task because the queue is empty.");
         }
      }

      public void Clear()
      {
         // Clear all tasks in the queue
         while (_tasks.TryDequeue(out var task))
         {
            _logger.LogInformation("Discarded: {TaskName}", task.Item1);
         }

         // Dispose of and remove all associated timers
         foreach (var key in _timers.Keys)
         {
            if (_timers.TryRemove(key, out var timer))
            {
               timer.Dispose();
            }
         }

         // Reset the semaphore to avoid releasing unnecessary signals
         while (_signal.CurrentCount > 0)
         {
            _signal.Wait(0);
         }

         _logger.LogInformation("Background Task Queue Cleared.");
      }
   }
}
