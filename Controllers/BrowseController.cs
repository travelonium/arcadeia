using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace MediaCurator.Controllers
{
   [ApiController]
   [Route("[controller]/{*path}")]
   public class BrowseController : Controller
   {
      private readonly IMediaDatabase _mediaDatabase;
      private readonly ILogger<BrowseController> _logger;

      public BrowseController(ILogger<BrowseController> logger, IMediaDatabase mediaDatabase)
      {
         _logger = logger;
         _mediaDatabase = mediaDatabase;
      }

      /// <summary>
      /// GET: /<controller>/
      /// </summary>
      /// <param name="path"></param>
      /// <returns></returns>
      public IActionResult Index(string path = "")
      {
         // TODO: Add a recursive query parameter flag.

         var levels = path.Split('/').Where(item => !String.IsNullOrEmpty(item)).ToArray();

         return Content("Hello! The path is " + path + " and is " + string.Format("{0}", levels.Count()) + " levels deep.");
      }
   }
}
