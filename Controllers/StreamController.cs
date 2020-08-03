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
   [Route("[controller]/{id}/{name}")]
   public class StreamController : Controller
   {
      private readonly IMediaLibrary _mediaLibrary;
      private readonly IConfiguration _configuration;
      private readonly ILogger<StreamController> _logger;
      private readonly IThumbnailsDatabase _thumbnailsDatabase;

      public StreamController(ILogger<StreamController> logger, IConfiguration configuration, IThumbnailsDatabase thumbnailsDatabase, IMediaLibrary mediaLibrary)
      {
         _logger = logger;
         _mediaLibrary = mediaLibrary;
         _configuration = configuration;
         _thumbnailsDatabase = thumbnailsDatabase;
      }

      // GET: /<controller>/
      public IActionResult Index(string id, string name)
      {
         XElement element = Tools.GetElementByIdAttribute(MediaLibrary.Document.Root, "Video", id);

         if (element == null)
         {
            return NotFound();
         }

         VideoFile videoFile = new VideoFile(_configuration, _thumbnailsDatabase, element);

         if ((videoFile.Self == null) || (videoFile.Name != name) || (!videoFile.Exists()))
         {
            return NotFound();
         }

         return PhysicalFile(videoFile.FullPath, "application/octet-stream", true);
      }
   }
}
