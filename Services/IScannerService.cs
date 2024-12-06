namespace Arcadeia.Services
{
   public interface IScannerService: IHostedService
   {
      bool Updating { get; }

      bool Scanning { get; }

      public Task RestartAsync(CancellationToken cancellationToken);

      public Task ScanAsync(string uuid, string path, string type, CancellationToken cancellationToken);

      public Task UpdateAsync(string uuid, CancellationToken cancellationToken);
   }
}
