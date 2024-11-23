namespace MediaCurator.Services
{
   public interface IDownloadService: IHostedService
   {
      Task<string?> DownloadMediaFileAsync(string url, string path, IProgress<string>? progress = null, string template = "%(title)s.%(ext)s", bool overwrite = false);
      Task<string?> GetMediaFileNameAsync(string url, string template = "%(title)s.%(ext)s");
   }
}