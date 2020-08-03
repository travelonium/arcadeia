using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

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

         if ((System.IO.Directory.Exists(Platform.Separator.Path + path) || (System.IO.File.Exists(Platform.Separator.Path + path))))
         {
            var mediaContainers = _mediaLibrary.ListMediaContainers(Platform.Separator.Path + path, progress);

            return mediaContainers.Select(item => item.Model);
         }

         return new List<Models.MediaContainer>();
      }
   }
}
