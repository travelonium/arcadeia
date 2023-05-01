﻿using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Text.RegularExpressions;

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
   }
}
