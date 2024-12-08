/* 
 *  Copyright © 2024 Travelonium AB
 *  
 *  This file is part of Arcadeia.
 *  
 *  Arcadeia is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU Affero General Public License as published
 *  by the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *  
 *  Arcadeia is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU Affero General Public License for more details.
 *  
 *  You should have received a copy of the GNU Affero General Public License
 *  along with Arcadeia. If not, see <https://www.gnu.org/licenses/>.
 *  
 */

using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ImageMagick;
using Arcadeia.Configuration;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Arcadeia.Controllers
{
   [ApiController]
   [Route("api/[controller]")]
   public class ThumbnailsController(ILogger<MediaContainer> logger,
                                     IServiceProvider services,
                                     IHostApplicationLifetime applicationLifetime,
                                     IThumbnailsDatabase thumbnailsDatabase,
                                     IOptionsMonitor<Settings> settings,
                                     IMediaLibrary mediaLibrary) : Controller
   {
      #region Constants

      #endregion // Constants

      private readonly IServiceProvider _services = services;
      private readonly IMediaLibrary _mediaLibrary = mediaLibrary;
      private readonly IOptionsMonitor<Settings> _settings = settings;
      private readonly ILogger<MediaContainer> _logger = logger;
      private readonly IThumbnailsDatabase _thumbnailsDatabase = thumbnailsDatabase;
      private readonly CancellationToken _cancellationToken = applicationLifetime.ApplicationStopping;

      // GET: /api/thumbnails/{id}/{index}.jpg
      [Route("{id}/{label}.jpg")]
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

      // GET: /api/thumbnails/{id}/{index}.vtt
      [Route("{id}/{label}.vtt")]
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

      // GET: /api/thumbnails/{id}
      [Route("{id}")]
      public async Task<IActionResult> Index(string id)
      {
         return await Index(id, "0");
      }
   }
}
