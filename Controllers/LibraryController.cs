using System;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace MediaCurator.Controllers
{
   [ApiController]
   [Route("[controller]/{*path}")]
   public class LibraryController : Controller
   {
      private readonly IServiceProvider _services;
      private readonly IMediaLibrary _mediaLibrary;
      private readonly IConfiguration _configuration;
      private readonly ILogger<MediaContainer> _logger;
      private readonly IThumbnailsDatabase _thumbnailsDatabase;

      public LibraryController(ILogger<MediaContainer> logger,
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

      [HttpPut]
      [Produces("application/json")]
      public IActionResult Put([FromBody] Models.MediaContainer modified, string path = "")
      {
         path = Platform.Separator.Path + path;

         try
         {
            if (!(System.IO.Directory.Exists(path)) && !(System.IO.File.Exists(path)))
            {
               return NotFound(new
               {
                  message = "File or folder not found."
               });
            }

            MediaContainer mediaContainer = new(_logger, _services, _configuration, _thumbnailsDatabase, _mediaLibrary, path: path);

            mediaContainer.Model = modified;

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
