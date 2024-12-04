using System;
using System.Threading;
using System.Threading.Tasks;

namespace MediaCurator
{
   public interface IBackgroundTaskQueue
   {
      IEnumerable<string> Tasks { get; }
      Task<Func<CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken);
      void Queue(string key, Func<CancellationToken, Task> task);
      public void Clear();
   }
}