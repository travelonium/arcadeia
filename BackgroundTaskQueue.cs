using System.Collections.Concurrent;

namespace MediaCurator
{
   public class BackgroundTaskQueue(ILogger<BackgroundTaskQueue> logger) : IBackgroundTaskQueue
   {
      private readonly ILogger<BackgroundTaskQueue> _logger = logger;
      private readonly ConcurrentQueue<Tuple<string, Func<CancellationToken, Task>>> _tasks = new();
      private readonly ConcurrentDictionary<string, Timer> _timers = new();
      private readonly SemaphoreSlim _signal = new(0);

      public void QueueBackgroundTask(string key, Func<CancellationToken, Task> task)
      {
         ArgumentNullException.ThrowIfNull(task, nameof(task));

         // Make sure the item has not already been added to the queue.
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
   }
}
