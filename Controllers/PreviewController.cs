using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Text.RegularExpressions;
using ImageMagick;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Text;
using System.Xml.Linq;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace MediaCurator.Controllers
{
   [ApiController]
   [Route("[controller]")]
   public class PreviewController : Controller
   {
      private readonly IServiceProvider _services;
      private readonly IMediaLibrary _mediaLibrary;
      private readonly IConfiguration _configuration;
      private readonly ILogger<MediaContainer> _logger;
      private readonly IThumbnailsDatabase _thumbnailsDatabase;

      #region Constants

      private Lazy<int> StreamingSegmentsDuration => new(() =>
      {
         var section = _configuration.GetSection("Streaming:Segments");
         return section.GetValue<int>("Duration");
      });

      #endregion // Constants

      public PreviewController(ILogger<MediaContainer> logger,
                               IServiceProvider services,
                               IConfiguration configuration,
                               IThumbnailsDatabase thumbnailsDatabase,
                               IMediaLibrary mediaLibrary)
      {
         _logger = logger;
         _services = services;
         _mediaLibrary = mediaLibrary;
         _configuration = configuration;
         _thumbnailsDatabase = thumbnailsDatabase;
      }

      // GET: /<controller>/video/{id}/{name}
      [HttpGet]
      [Route("video/{id}/{name}")]
      public IActionResult Video(string id, string name)
      {
         using VideoFile videoFile = new(_logger, _services, _configuration, _thumbnailsDatabase, _mediaLibrary, id: id);

         // Increment the Views only if this is the first ranged request or not ranged at all
         if (Request.Headers.TryGetValue("Range", out var range))
         {
            // Does the range header contain bytes=0- or similar?
            Regex pattern = new Regex(@"(?:\w.*)=0-(?:\d*)?", RegexOptions.Compiled | RegexOptions.IgnoreCase);

            if (pattern.IsMatch(range))
            {
               videoFile.Views += 1;
            }
         }
         else
         {
            videoFile.Views += 1;
         }

         if ((videoFile.Name != name) || (!videoFile.Exists()))
         {
            videoFile.Skipped = true;

            return NotFound();
         }

         return PhysicalFile(videoFile.FullPath, "application/octet-stream", true);
      }

      // GET: /<controller>/photo/{id}/{name}
      [HttpGet]
      [Route("photo/{id}/{name}")]
      public IActionResult Photo(string id, string name, [FromQuery] int width = 0, [FromQuery] int height = 0)
      {
         using PhotoFile photoFile = new(_logger, _services, _configuration, _thumbnailsDatabase, _mediaLibrary, id: id);

         if ((photoFile.Name != name) || (!photoFile.Exists()))
         {
            photoFile.Skipped = true;

            return NotFound();
         }

         photoFile.Views += 1;

         return photoFile.Extension switch
         {
            ".gif" => File(photoFile.Preview(width, height, ImageMagick.MagickFormat.Gif), "image/gif"),
            ".webp" => File(photoFile.Preview(width, height, ImageMagick.MagickFormat.WebP), "image/webp"),
            ".png" or ".bmp" or ".tiff" or ".tga" => File(photoFile.Preview(width, height, ImageMagick.MagickFormat.Png), "image/png"),
            _ => File(photoFile.Preview(width, height, ImageMagick.MagickFormat.Jpeg), "image/jpeg"),
         };
      }

      // GET: /<controller>/video/{id}/{quality}.m3u8
      [HttpGet]
      [Route("video/{id}/{quality}.m3u8")]
      public IActionResult Stream(string id, string quality)
      {
         using VideoFile videoFile = new(_logger, _services, _configuration, _thumbnailsDatabase, _mediaLibrary, id: id);

         videoFile.Views += 1;

         if (!videoFile.Exists())
         {
            videoFile.Skipped = true;

            return NotFound();
         }

         return quality.ToLower() switch
         {
            "master" => Content(videoFile.GeneratePlaylist(), "application/x-mpegURL", Encoding.UTF8),
            "240p" or "240" or "360p" or "360" or "480p" or "480" or "720p" or "720" or "1080p" or "1080" or "4k" or "2160" or "2160p" => Content(videoFile.GeneratePlaylist(StreamingSegmentsDuration.Value, quality), "application/x-mpegURL", Encoding.UTF8),
            _ => Content(videoFile.GeneratePlaylist(StreamingSegmentsDuration.Value), "application/x-mpegURL", Encoding.UTF8),
         };
      }

      // GET: /<controller>/video/{id}/{sequence}.ts
      [HttpGet]
      [Route("video/{id}/{sequence}.ts")]
      public IActionResult Segment(string id, int sequence)
      {
         using VideoFile videoFile = new(_logger, _services, _configuration, _thumbnailsDatabase, _mediaLibrary, id: id);

         if (!videoFile.Exists())
         {
            videoFile.Skipped = true;

            return NotFound();
         }

         return File(videoFile.GenerateSegments("", sequence, StreamingSegmentsDuration.Value, 1), "application/x-mpegURL", true);
      }

      // GET: /<controller>/video/{id}/{quality}/{sequence}.ts
      [HttpGet]
      [Route("video/{id}/{quality}/{sequence}.ts")]
      public IActionResult Segment(string id, string quality, int sequence)
      {
         using VideoFile videoFile = new(_logger, _services, _configuration, _thumbnailsDatabase, _mediaLibrary, id: id);

         if (!videoFile.Exists())
         {
            videoFile.Skipped = true;

            return NotFound();
         }

         return File(videoFile.GenerateSegments(quality, sequence, StreamingSegmentsDuration.Value, 1), "application/x-mpegURL", true);
      }
   }
}
