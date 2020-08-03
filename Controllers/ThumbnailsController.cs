using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
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
      private readonly CancellationToken _cancellationToken;

      public ThumbnailsController(ILogger<ThumbnailsController> logger,
                                  IHostApplicationLifetime applicationLifetime,
                                  IThumbnailsDatabase thumbnailsDatabase,
                                  IMediaLibrary mediaLibrary)
      {
         _logger = logger;
         _mediaLibrary = mediaLibrary;
         _thumbnailsDatabase = thumbnailsDatabase;
         _cancellationToken = applicationLifetime.ApplicationStopping;
      }

      // GET: /<controller>/{id}/{index}
      [Route("[controller]/{id}/{index}.jpg")]
      public async Task<IActionResult> Index(string id, int index)
      {
         var thumbnail = await _thumbnailsDatabase.GetThumbnailAsync(id, index, _cancellationToken);

         if (thumbnail.Length > 0)
         {
            Response.Headers.Add("Cache-Control", "max-age=86400");
            return File(thumbnail, "image/jpeg");
         }

         return NotFound();
      }

      // GET: /<controller>/{id}
      [Route("[controller]/{id}")]
      public async Task<IActionResult> Index(string id)
      {
         return await Index(id, 0);
      }
   }
}
