using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using System.Xml.Linq;
using Microsoft.Extensions.Configuration;
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using MediaCurator.Solr;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace MediaCurator.Controllers
{
   [ApiController]
   [Route("[controller]/{*path}")]
   public class LibraryController : Controller
   {
      private readonly IMediaLibrary _mediaLibrary;
      private readonly IConfiguration _configuration;
      private readonly IServiceProvider _serviceProvider;
      private readonly ILogger<LibraryController> _logger;
      private readonly IThumbnailsDatabase _thumbnailsDatabase;

      public LibraryController(IConfiguration configuration,
                               ILogger<LibraryController> logger,
                               IThumbnailsDatabase thumbnailsDatabase,
                               IServiceProvider serviceProvider,
                               IMediaLibrary mediaLibrary)
      {
         _logger = logger;
         _mediaLibrary = mediaLibrary;
         _configuration = configuration;
         _serviceProvider = serviceProvider;
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

            mediaContainer.Model = modified;

            // Update the media in the Solr index if indexing is enabled.
            if (_configuration.GetSection("Solr:URL").Exists())
            {
               using (IServiceScope scope = _serviceProvider.CreateScope())
               {
                  ISolrIndexService<Models.MediaContainer> solrIndexService = scope.ServiceProvider.GetRequiredService<ISolrIndexService<Models.MediaContainer>>();
                  solrIndexService.Update(mediaContainer.Model);
               }
            }

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
