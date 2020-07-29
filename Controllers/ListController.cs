using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using System.Text.Json;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace MediaCurator.Controllers
{
   [ApiController]
   [Route("[controller]/{*path}")]
   public class ListController : Controller
   {
      private readonly IMediaLibrary _mediaLibrary;
      private readonly ILogger<ListController> _logger;

      public ListController(ILogger<ListController> logger, IMediaLibrary mediaLibrary)
      {
         _logger = logger;
         _mediaLibrary = mediaLibrary;
      }

      /// <summary>
      /// GET: /<controller>/
      /// </summary>
      /// <param name="path">The path to the folder or file which is the parent or the destination of the list.</param>
      /// <returns></returns>
      public IEnumerable<Models.MediaContainer> Index(string path = "")
      {
         var progress = new Progress<Tuple<double, double, string>>();
         var levels = path.Split('/').Where(item => !String.IsNullOrEmpty(item)).ToArray();

         // return Content("Hello! The path is " + path + " and is " + string.Format("{0}", levels.Count()) + " levels deep.");
         // return File(mediaFile.Thumbnails[0], "image/jpeg");

         var mediaContainers = _mediaLibrary.ListMediaContainers(Platform.Separator.Path + path, progress);

         return mediaContainers.Select(item => item.Model);
      }
   }
}
