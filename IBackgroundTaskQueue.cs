using System;
using System.Threading;
using System.Threading.Tasks;

namespace MediaCurator
{
   public interface IBackgroundTaskQueue
   {
      Task<Func<CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken);
      void QueueBackgroundTask(string key, Func<CancellationToken, Task> task);
   }
}