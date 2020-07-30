using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace MediaCurator.Controllers
{
   [ApiController]
   public class ThumbnailsController : Controller
   {
      private readonly IMediaLibrary _mediaLibrary;
      private readonly IThumbnailsDatabase _thumbnailsDatabase;
      private readonly ILogger<ThumbnailsController> _logger;

      public ThumbnailsController(ILogger<ThumbnailsController> logger, IMediaLibrary mediaLibrary, IThumbnailsDatabase thumbnailsDatabase)
      {
         _logger = logger;
         _mediaLibrary = mediaLibrary;
         _thumbnailsDatabase = thumbnailsDatabase;
      }

      // GET: /<controller>/{id}/{index}
      [Route("[controller]/{id}/{index}")]
      public IActionResult Index(string id, int index)
      {
         var thumbnail = _thumbnailsDatabase.GetThumbnail(id, index);

         if (thumbnail.Length > 0)
         {
            return File(thumbnail, "image/jpeg");
         }

         return NotFound();
      }

      // GET: /<controller>/{id}
      [Route("[controller]/{id}")]
      public IActionResult Index(string id)
      {
         return Index(id, 0);
      }
   }
}
