using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MediaCurator;
using System.Xml.Linq;
using Microsoft.Extensions.Configuration;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace MediaCurator.Controllers
{
   [ApiController]
   [Route("[controller]")]
   public class PreviewController : Controller
   {
      private readonly IMediaLibrary _mediaLibrary;
      private readonly IConfiguration _configuration;
      private readonly ILogger<PreviewController> _logger;
      private readonly IThumbnailsDatabase _thumbnailsDatabase;

      public PreviewController(ILogger<PreviewController> logger, IConfiguration configuration, IThumbnailsDatabase thumbnailsDatabase, IMediaLibrary mediaLibrary)
      {
         _logger = logger;
         _mediaLibrary = mediaLibrary;
         _configuration = configuration;
         _thumbnailsDatabase = thumbnailsDatabase;
      }

      // GET: /<controller>/video/{id}/{name}
      [HttpGet]
      [Route("video/{id}/{name}")]
      public IActionResult Video(string id, string name)
      {
         XElement element = Tools.GetElementByIdAttribute(_mediaLibrary.Self, "Video", id);

         if (element == null)
         {
            return NotFound();
         }

         VideoFile videoFile = new VideoFile(_configuration, _thumbnailsDatabase, _mediaLibrary, element);

         if ((videoFile.Self == null) || (videoFile.Name != name) || (!videoFile.Exists()))
         {
            return NotFound();
         }

         return PhysicalFile(videoFile.FullPath, "application/octet-stream", true);
      }

      // GET: /<controller>/photo/{id}/{name}
      [HttpGet]
      [Route("photo/{id}/{name}")]
      public IActionResult Photo(string id, string name, [FromQuery] int width = 0, [FromQuery] int height = 0)
      {
         XElement element = Tools.GetElementByIdAttribute(_mediaLibrary.Self, "Photo", id);

         if (element == null)
         {
            return NotFound();
         }

         PhotoFile photoFile = new(_configuration, _thumbnailsDatabase, _mediaLibrary, element);

         if ((photoFile.Self == null) || (photoFile.Name != name) || (!photoFile.Exists()))
         {
            return NotFound();
         }

         string contentType = (photoFile.ContentType != "") ? photoFile.ContentType : "application/octet-stream";

         if ((width > 0) || (height > 0))
         {
            return File(photoFile.Preview(width, height), "image/png");
         }

         return PhysicalFile(photoFile.FullPath, contentType, true);
      }
   }
}
