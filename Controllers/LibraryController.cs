using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using System.Xml.Linq;
using Microsoft.Extensions.Configuration;
using System.Net;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace MediaCurator.Controllers
{
   [ApiController]
   [Route("[controller]/{*path}")]
   public class LibraryController : Controller
   {
      private readonly IMediaLibrary _mediaLibrary;
      private readonly IConfiguration _configuration;
      private readonly IThumbnailsDatabase _thumbnailsDatabase;
      private readonly ILogger<LibraryController> _logger;

      public LibraryController(ILogger<LibraryController> logger, IConfiguration configuration, IThumbnailsDatabase thumbnailsDatabase, IMediaLibrary mediaLibrary)
      {
         _logger = logger;
         _mediaLibrary = mediaLibrary;
         _configuration = configuration;
         _thumbnailsDatabase = thumbnailsDatabase;
      }

      /// <summary>
      /// GET: /<controller>/
      /// </summary>
      /// <param name="path">The path to the folder or file which is the parent or the destination of the list.</param>
      /// <returns></returns>
      [HttpGet]
      [Produces("application/json")]
      public IActionResult Get(string path = "", [FromQuery] string query = null, [FromQuery] uint flags = 0, [FromQuery] uint values = 0, [FromQuery] bool recursive = false)
      {
         path = Platform.Separator.Path + path;

         try
         {
            if (System.IO.File.Exists(path))
            {
               var mediaContainers = _mediaLibrary.ListMediaContainers(path, query, flags, values, recursive);

               if (mediaContainers.Count == 1)
               {
                  return Ok(mediaContainers.First().Model);
               }
            }
            else if (System.IO.Directory.Exists(path))
            {
               var mediaContainers = _mediaLibrary.ListMediaContainers(path, query, flags, values, recursive);

               return Ok(mediaContainers.Select(item => item.Model));
            }
         }
         catch (Exception e)
         {
            return StatusCode((int)HttpStatusCode.InternalServerError, new
            {
               message = e.Message
            });
         }

         return Ok(new List<Models.MediaContainer>());
      }

      [HttpPut]
      [Produces("application/json")]
      public IActionResult Put([FromBody] Models.MediaContainer modified, string path = "")
      {
         path = Platform.Separator.Path + path;

         try
         {
            IMediaContainer mediaContainer = new MediaContainer(_configuration, _thumbnailsDatabase, _mediaLibrary, path);

            // In case of a Server or a Drive or a Folder located in the root, the mediaContainer's Self
            // will be null and it only will have found a Parent element which is the one we need. As a
            // workaround, we replace the item with its parent.

            if ((mediaContainer.Self == null) && (mediaContainer.Parent != null))
            {
               mediaContainer = mediaContainer.Parent;
            }

            if (!(System.IO.Directory.Exists(path)) && !(System.IO.File.Exists(path)))
            {
               return NotFound(new
               {
                  message = "File or folder not found."
               });
            }

            var id = modified.Id;

            mediaContainer.Model = modified;

            _mediaLibrary.UpdateDatabase();

            return Ok(mediaContainer.Model);
         }
         catch (Exception e)
         {
            return StatusCode((int)HttpStatusCode.InternalServerError, new
            {
               message = e.Message
            });
         }
      }
   }
}
