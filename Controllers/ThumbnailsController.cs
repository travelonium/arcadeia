using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ImageMagick;
using MediaCurator.Configuration;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace MediaCurator.Controllers
{
   [ApiController]
   public class ThumbnailsController : Controller
   {
      #region Constants

      #endregion // Constants

      private readonly IServiceProvider _services;
      private readonly IMediaLibrary _mediaLibrary;
      private readonly IOptionsMonitor<Settings> _settings;
      private readonly ILogger<MediaContainer> _logger;
      private readonly IThumbnailsDatabase _thumbnailsDatabase;
      private readonly CancellationToken _cancellationToken;

      public ThumbnailsController(ILogger<MediaContainer> logger,
                                  IServiceProvider services,
                                  IHostApplicationLifetime applicationLifetime,
                                  IThumbnailsDatabase thumbnailsDatabase,
                                  IOptionsMonitor<Settings> settings,
                                  IMediaLibrary mediaLibrary)
      {
         _logger = logger;
         _services = services;
         _mediaLibrary = mediaLibrary;
         _settings = settings;
         _thumbnailsDatabase = thumbnailsDatabase;
         _cancellationToken = applicationLifetime.ApplicationStopping;
      }

      // GET: /<controller>/{id}/{index}.jpg
      [Route("[controller]/{id}/{label}.jpg")]
      public async Task<IActionResult> Index(string id, string label)
      {
         byte[] thumbnail;
         var pattern = new Regex("^\\d*$", RegexOptions.IgnoreCase);

         if (pattern.IsMatch(label))
         {
            var index = Convert.ToInt32(label);
            thumbnail = await _thumbnailsDatabase.GetThumbnailAsync(id, index, _cancellationToken);
         }
         else
         {
            thumbnail = await _thumbnailsDatabase.GetThumbnailAsync(id, label, _cancellationToken);
         }

         if (thumbnail.Length > 0)
         {
            Response.Headers.CacheControl = "max-age=86400";

            return File(thumbnail, "image/jpeg");
         }

         return NotFound();
      }

      // GET: /<controller>/{id}/{index}.vtt
      [Route("[controller]/{id}/{label}.vtt")]
      public async Task<IActionResult> WebVTT(string id, string label)
      {
         byte[] thumbnail;
         var content = new StringBuilder();

         var item = _settings.CurrentValue.Thumbnails.Video.FirstOrDefault(x => string.Equals(x.Key, label, StringComparison.OrdinalIgnoreCase)).Value;

         if (item is null)
         {
            return NotFound();
         }

         VideoFile videoFile = new(_logger, _services, _settings, _thumbnailsDatabase, _mediaLibrary, id: id);

         if ((videoFile.Type != MediaContainerType.Video.ToString()) || (videoFile.Duration <= 0))
         {
            return NotFound();
         }

         long width = item.Width;
         long height = item.Height;
         bool sprite = item.Sprite;
         double duration = videoFile.Duration;
         string from = "00:00:00";
         int count = (item.Count > 0) ? (int)Math.Min(item.Count, Math.Floor(duration)) : 0;

         if (!sprite || (count <= 1))
         {
            return NotFound();
         }

         thumbnail = await _thumbnailsDatabase.GetThumbnailAsync(id, label, _cancellationToken);

         if (thumbnail.Length <= 0)
         {
            return NotFound();
         }

         var info = new MagickImageInfo(thumbnail);

         if ((width <= 0) && (info.Width > 0)) width = (long)(info.Width / count);
         if ((height <= 0) && (info.Height > 0)) height = (long)(info.Height);

         content.AppendLine("WEBVTT" + Environment.NewLine);

         for (int counter = 0; counter < count; counter++)
         {
            var y = 0;
            var x = counter * width;
            string to = TimeSpan.FromSeconds((counter + 0.5) * duration / count).ToString(@"hh\:mm\:ss");

            content.AppendLine(string.Format("{0}.000 --> {1}.000", from, to));
            content.AppendLine(string.Format("{0}.jpg#xywh={1},{2},{3},{4}", label, x, y, width, height) + Environment.NewLine);

            from = to;
         }

         return Content(content.ToString(), "text/plain", Encoding.UTF8);
      }

      // GET: /<controller>/{id}
      [Route("[controller]/{id}")]
      public async Task<IActionResult> Index(string id)
      {
         return await Index(id, "0");
      }
   }
}
