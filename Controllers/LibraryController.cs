using System;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.FileSystemGlobbing.Internal;
using System.Text.RegularExpressions;

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

      /// <summary>
      /// A list of regex patterns specifying which file names to ignore when scanning.
      /// </summary>
      public List<String> IgnoredFileNames
      {
         get
         {
            if (_configuration.GetSection("Scanner:IgnoredFileNames").Exists())
            {
               return _configuration.GetSection("Scanner:IgnoredFileNames").Get<List<String>>();
            }
            else
            {
               return new List<String>();
            }
         }
      }

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

      [HttpPatch]
      [Produces("application/json")]
      public IActionResult Patch([FromBody] Models.MediaContainer modified, string path = "")
      {
         path = Platform.Separator.Path + path;

         try
         {
            if ((!System.IO.Directory.Exists(path) && !System.IO.File.Exists(path)) || String.IsNullOrEmpty(modified.Id) || String.IsNullOrEmpty(modified.Type))
            {
               return NotFound(new
               {
                  message = "File or folder not found."
               });
            }

            var type = modified.Type.ToEnum<MediaContainerType>().ToType();

            // Create the parent container of the right type.
            using IMediaContainer mediaContainer = (MediaContainer) Activator.CreateInstance(type, _logger, _services, _configuration, _thumbnailsDatabase, _mediaLibrary, modified.Id, null);

            // Reset the Views to its currently stored value as the UI is not allowed to update it.
            modified.Views = mediaContainer.Model.Views;

            // Update the container with the modified version.
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

      /// <summary>
      /// Upload a file failing if a file with the same name already exists.
      /// </summary>
      /// <param name="files"></param>
      /// <param name="path"></param>
      /// <returns></returns>
      [HttpPost]
      [Produces("application/json")]
      [RequestSizeLimit(1 * 1024 * 1024 * 1024)]
      [RequestFormLimits(MultipartBodyLengthLimit = 1 * 1024 * 1024 * 1024)]
      public async Task<IActionResult> Post(List<IFormFile> files, string path = "")
      {
         path = Platform.Separator.Path + path;
         long size = files.Sum(file => file.Length);

         if (!System.IO.Directory.Exists(path))
         {
            return NotFound(new
            {
               message = "Destination folder not found."
            });
         }

         foreach (var file in files)
         {
            var fullPath = Path.Combine(path, file.FileName);

            if (System.IO.File.Exists(fullPath))
            {
               return Conflict(new
               {
                  message = "Destination file already exists."
               });
            }

            if (file.Length > 0)
            {
               using (var stream = System.IO.File.Create(fullPath))
               {
                  await file.CopyToAsync(stream);

                  _logger.LogInformation("File Uploaded: {}", fullPath);
               }

               var name = Path.GetFileName(file.FileName);
               var patterns = IgnoredFileNames.Select(pattern => new Regex(pattern, RegexOptions.IgnoreCase)).ToList<Regex>();

               // Shouldn't the file name be ignored?
               if (!patterns.Any(pattern => (pattern.IsMatch(fullPath) || pattern.IsMatch(name))))
               {
                  try
                  {
                     // Add the file to the MediaLibrary.
                     using MediaFile newMediaFile = _mediaLibrary.InsertMedia(fullPath);
                  }
                  catch (Exception e)
                  {
                     _logger.LogWarning("Failed To Insert: {}, Because: {}", file, e.Message);
                     _logger.LogDebug("{}", e.ToString());
                  }
               }
               else
               {
                  _logger.LogDebug("File Ignored: {}", fullPath);
               }
            }
         }

         // Process uploaded files
         // Don't rely on or trust the FileName property without validation.

         return Ok();
      }

      [HttpPut]
      [Produces("application/json")]
      public IActionResult Put(List<IFormFile> files, string path = "")
      {
         // Upload a file overwriting existing files with the same name.
         return Ok();
      }
   }
}
