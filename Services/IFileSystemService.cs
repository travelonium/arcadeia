namespace Arcadeia.Services
{
   public interface IFileSystemService: IHostedService
   {
      List<FileSystemMount> Mounts { get; }

      public Task RestartAsync(CancellationToken cancellationToken);

      Task ReloadAsync(CancellationToken cancellationToken);
   }
}
