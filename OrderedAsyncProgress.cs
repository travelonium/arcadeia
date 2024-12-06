namespace Arcadeia
{
   public class OrderedAsyncProgress<T>(Func<T, Task> handler) : IProgress<T>
   {
      private readonly Func<T, Task> _handler = handler ?? throw new ArgumentNullException(nameof(handler));
      private readonly SemaphoreSlim _semaphore = new(1, 1);

      public void Report(T value)
      {
         // Queue the execution to ensure ordered processing
         _ = ProcessAsync(value);
      }

      private async Task ProcessAsync(T value)
      {
         await _semaphore.WaitAsync();
         try
         {
            await _handler(value);
         }
         finally
         {
            _semaphore.Release();
         }
      }
   }
}
