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
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc;
using Arcadeia.Configuration;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Arcadeia.Controllers
{
   [ApiController]
   [Route("api/[controller]")]
   public partial class PreviewController(ILogger<MediaContainer> logger,
                                          IServiceProvider services,
                                          IOptionsMonitor<Settings> settings,
                                          IThumbnailsDatabase thumbnailsDatabase,
                                          IMediaLibrary mediaLibrary) : Controller
   {
      private readonly IServiceProvider _services = services;
      private readonly IMediaLibrary _mediaLibrary = mediaLibrary;
      private readonly IOptionsMonitor<Settings> _settings = settings;
      private readonly ILogger<MediaContainer> _logger = logger;
      private readonly IThumbnailsDatabase _thumbnailsDatabase = thumbnailsDatabase;

      [GeneratedRegex(@"(?:\w.*)=0-(?:\d*)?")]
      private static partial Regex RangeRegex();

      #region Constants

      #endregion // Constants

      // GET: /api/preview/video/{id}/{name}
      [HttpGet]
      [Route("video/{id}/{name}")]
      public IActionResult Video(string id, string name)
      {
         using VideoFile videoFile = new(_logger, _services, _settings, _thumbnailsDatabase, _mediaLibrary, id: id);

         // Increment the Views only if this is the first ranged request or not ranged at all
         if (Request.Headers.TryGetValue("Range", out var range) && !string.IsNullOrEmpty(range))
         {
            // Does the range header contain bytes=0- or similar?
            if (RangeRegex().Match(range.ToString()).Success)
            {
               videoFile.Views += 1;
               videoFile.DateAccessed = DateTime.UtcNow;
            }
         }
         else
         {
            videoFile.Views += 1;
            videoFile.DateAccessed = DateTime.UtcNow;
         }

         if (string.IsNullOrEmpty(videoFile.FullPath) || (videoFile.Name != name) || (!videoFile.Exists()))
         {
            videoFile.Skipped = true;

            return NotFound();
         }

         return PhysicalFile(videoFile.FullPath, "application/octet-stream", true);
      }

      // GET: /api/preview/photo/{id}/{name}
      [HttpGet]
      [Route("photo/{id}/{name}")]
      public IActionResult Photo(string id, string name, [FromQuery] int width = 0, [FromQuery] int height = 0)
      {
         using PhotoFile photoFile = new(_logger, _services, _settings, _thumbnailsDatabase, _mediaLibrary, id: id);

         if ((photoFile.Name != name) || (!photoFile.Exists()))
         {
            photoFile.Skipped = true;

            return NotFound();
         }

         photoFile.Views += 1;
         photoFile.DateAccessed = DateTime.UtcNow;

         var fileContents = photoFile.Preview(width, height, photoFile.Extension switch
         {
            ".gif" => ImageMagick.MagickFormat.Gif,
            ".webp" => ImageMagick.MagickFormat.WebP,
            ".png" or ".bmp" or ".tiff" or ".tga" => ImageMagick.MagickFormat.Png,
            _ => ImageMagick.MagickFormat.Jpeg
         });

         if (fileContents == null)
         {
            return NotFound();
         }

         var contentType = photoFile.Extension switch
         {
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".png" or ".bmp" or ".tiff" or ".tga" => "image/png",
            _ => "image/jpeg"
         };

         return File(fileContents, contentType);
      }

      // GET: /api/preview/video/{id}/{quality}.m3u8
      [HttpGet]
      [Route("video/{id}/{quality}.m3u8")]
      public IActionResult Stream(string id, string quality)
      {
         using VideoFile videoFile = new(_logger, _services, _settings, _thumbnailsDatabase, _mediaLibrary, id: id);

         videoFile.Views += 1;
         videoFile.DateAccessed = DateTime.UtcNow;

         if (!videoFile.Exists())
         {
            videoFile.Skipped = true;

            return NotFound();
         }

         return quality.ToLower() switch
         {
            "master" => Content(videoFile.GeneratePlaylist(), "application/x-mpegURL", Encoding.UTF8),
            "240p" or "240" or "360p" or "360" or "480p" or "480" or "720p" or "720" or "1080p" or "1080" or "4k" or "2160" or "2160p" => Content(videoFile.GeneratePlaylist(_settings.CurrentValue.Streaming.Segments.Duration, quality), "application/x-mpegURL", Encoding.UTF8),
            _ => Content(videoFile.GeneratePlaylist(_settings.CurrentValue.Streaming.Segments.Duration), "application/x-mpegURL", Encoding.UTF8),
         };
      }

      // GET: /api/preview/video/{id}/{sequence}.ts
      [HttpGet]
      [Route("video/{id}/{sequence}.ts")]
      public IActionResult Segment(string id, int sequence)
      {
         using VideoFile videoFile = new(_logger, _services, _settings, _thumbnailsDatabase, _mediaLibrary, id: id);

         if (!videoFile.Exists())
         {
            videoFile.Skipped = true;

            return NotFound();
         }

         return File(videoFile.GenerateSegments("", sequence, _settings.CurrentValue.Streaming.Segments.Duration, 1), "application/x-mpegURL", true);
      }

      // GET: /api/preview/video/{id}/{quality}/{sequence}.ts
      [HttpGet]
      [Route("video/{id}/{quality}/{sequence}.ts")]
      public IActionResult Segment(string id, string quality, int sequence)
      {
         using VideoFile videoFile = new(_logger, _services, _settings, _thumbnailsDatabase, _mediaLibrary, id: id);

         if (!videoFile.Exists())
         {
            videoFile.Skipped = true;

            return NotFound();
         }

         return File(videoFile.GenerateSegments(quality, sequence, _settings.CurrentValue.Streaming.Segments.Duration, 1), "application/x-mpegURL", true);
      }
   }
}
