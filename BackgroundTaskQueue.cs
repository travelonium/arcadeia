using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;

namespace MediaCurator
{
   public class BackgroundTaskQueue : IBackgroundTaskQueue
   {
      private ConcurrentQueue<Tuple<string, Func<CancellationToken, Task>>> _tasks = new();
      private ConcurrentDictionary<string, Timer> _timers = new();
      private SemaphoreSlim _signal = new(0);

      public void QueueBackgroundTask(string key, Func<CancellationToken, Task> task)
      {
         if (task == null)
         {
            throw new ArgumentNullException(nameof(task));
         }

         // Make sure the item has not already been added to the queue.
         foreach (var entry in _tasks)
         {
            if (entry.Item1 == key)
            {
               return;
            }
         }

         if (_timers.ContainsKey(key))
         {
            _timers[key].Dispose();
            _timers.Remove(key, out _);
         }

         _timers.TryAdd(key, new Timer((_) =>
         {
            _tasks.Enqueue(new Tuple<string, Func<CancellationToken, Task>>(key, task));

            Debug.WriteLine("QUEUED: " + key);

            _signal.Release();

         }, null, 15000, Timeout.Infinite));
      }

      public async Task<Func<CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken)
      {
         await _signal.WaitAsync(cancellationToken);

         _tasks.TryDequeue(out var task);

         Debug.WriteLine("DEQUEUED: " + task.Item1);

         return task.Item2;
      }
   }
}
