using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using static System.Net.Mime.MediaTypeNames;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using static System.Collections.Specialized.BitVector32;
using System.Linq;
using System.Text;
using ImageMagick;
using System.IO;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace MediaCurator.Controllers
{
   [ApiController]
   public class ThumbnailsController : Controller
   {
      #region Constants

      private Lazy<Dictionary<string, Dictionary<string, int>>> ThumbnailsConfiguration => new(() =>
      {
         var comparer = StringComparer.OrdinalIgnoreCase;

         return new Dictionary<string, Dictionary<string, int>>(_configuration.GetSection("Thumbnails:Video").Get<Dictionary<string, Dictionary<string, int>>>() ?? [], comparer);
      });

      #endregion // Constants

      private readonly IServiceProvider _services;
      private readonly IMediaLibrary _mediaLibrary;
      private readonly IConfiguration _configuration;
      private readonly ILogger<MediaContainer> _logger;
      private readonly IThumbnailsDatabase _thumbnailsDatabase;
      private readonly CancellationToken _cancellationToken;

      public ThumbnailsController(ILogger<MediaContainer> logger,
                                  IServiceProvider services,
                                  IHostApplicationLifetime applicationLifetime,
                                  IThumbnailsDatabase thumbnailsDatabase,
                                  IConfiguration configuration,
                                  IMediaLibrary mediaLibrary)
      {
         _logger = logger;
         _services = services;
         _mediaLibrary = mediaLibrary;
         _configuration = configuration;
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

         if (!ThumbnailsConfiguration.Value.ContainsKey(label))
         {
            return NotFound();
         }

         VideoFile videoFile = new(_logger, _services, _configuration, _thumbnailsDatabase, _mediaLibrary, id: id);

         if ((videoFile.Type != MediaContainerType.Video.ToString()) || (videoFile.Duration <= 0))
         {
            return NotFound();
         }

         var item = ThumbnailsConfiguration.Value.GetValueOrDefault(label);

         int count = 0;
         string from = "00:00:00";
         double duration = videoFile.Duration;
         long width = item?.GetValueOrDefault("Width") ?? -1;
         long height = item?.GetValueOrDefault("Height") ?? -1;
         bool sprite = item?.GetValueOrDefault("Sprite") > 0;

         if (item?.ContainsKey("Count") == true)
         {
            count = (int)Math.Min(item["Count"], Math.Floor(duration));
         }

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
