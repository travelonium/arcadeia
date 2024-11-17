namespace MediaCurator.Services
{
   public interface IDownloadService: IHostedService
   {
      Task<string?> DownloadMediaFile(string url, string path, IProgress<string>? progress = null);
      Task<string?> GetMediaFileNameAsync(string url);
   }
}