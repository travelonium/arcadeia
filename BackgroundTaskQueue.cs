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
      private ConcurrentQueue<Tuple<string, Func<CancellationToken, Task>>> _workItems = new ConcurrentQueue<Tuple<string, Func<CancellationToken, Task>>>();
      private SemaphoreSlim _signal = new SemaphoreSlim(0);

      public void QueueBackgroundWorkItem(string key, Func<CancellationToken, Task> workItem)
      {
         if (workItem == null)
         {
            throw new ArgumentNullException(nameof(workItem));
         }

         // Make sure the item has not already been added to the queue.
         foreach (var item in _workItems)
         {
            if (item.Item1 == key)
            {
               return;
            }
         }

         Debug.WriteLine("QUEUED: " + key);

         _workItems.Enqueue(new Tuple<string, Func<CancellationToken, Task>>(key, workItem));
         _signal.Release();
      }

      public async Task<Func<CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken)
      {
         await _signal.WaitAsync(cancellationToken);

         _workItems.TryDequeue(out var workItem);

         return workItem.Item2;
      }
   }
}
