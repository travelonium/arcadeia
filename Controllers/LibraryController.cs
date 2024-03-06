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
using MediaCurator.Solr;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace MediaCurator.Controllers
{
   [ApiController]
   [Route("[controller]")]
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
      [Route("{*path}")]
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
      [Route("{*path}")]
      [Produces("application/json")]
      [RequestSizeLimit(10L * 1024 * 1024 * 1024)]
      [RequestFormLimits(MultipartBodyLengthLimit = 10L * 1024 * 1024 * 1024)]
      public async Task<IActionResult> Post(List<IFormFile> files, string path = "", [FromQuery] bool overwrite = false, [FromQuery] bool duplicate = false)
      {
         path = Platform.Separator.Path + path;
         long size = files.Sum(file => file.Length);
         var result = new List<Models.MediaContainer>();

         if (overwrite && duplicate)
         {
            return Problem(title: "Bad Request", detail: "Either overwrite or duplicate must be false.", statusCode: 400);
         }

         if (!System.IO.Directory.Exists(path))
         {
            return Problem(title: "Folder Not Found",detail: "Destination folder not found.", statusCode: 404);
         }

         foreach (var file in files)
         {
            var name = Path.GetFileName(file.FileName);
            var fullPath = Path.Combine(path, file.FileName);
            var patterns = IgnoredFileNames.Select(pattern => new Regex(pattern, RegexOptions.IgnoreCase)).ToList<Regex>();

            if (System.IO.File.Exists(fullPath))
            {
               if (overwrite)
               {
                  // It shall be overwritten then.
               }
               else if (duplicate)
               {
                  Regex pattern = new Regex(@"(.*)(\s+\(\d+\))");
                  var extension = Path.GetExtension(file.FileName);
                  var fileName = Path.GetFileNameWithoutExtension(file.FileName);
                  var match = pattern.Match(fileName);

                  if (match.Success)
                  {
                     // The file name already has an (x) index in the end.
                     fileName = match.Groups[1].Value;
                  }

                  // Try to find an index that is unique.
                  for (int i = 1; i < Int32.MaxValue; i++)
                  {
                     if (!System.IO.File.Exists(Path.Combine(path, fileName + "(" + i + ")" + extension)) &&
                         !System.IO.File.Exists(Path.Combine(path, fileName + " (" + i + ")" + extension)))
                     {
                        fullPath = Path.Combine(path, fileName + " (" + i + ")" + extension);
                        break;
                     }
                  }
               }
               else
               {
                  if (files.Count == 1)
                  {
                     return Problem(title: "Already Exists", detail: "Destination file already exists.", statusCode: 409);
                  }
                  else continue;
               }
            }

            // Shouldn't the file name be ignored?
            if ((file.Length <= 0) || patterns.Any(pattern => (pattern.IsMatch(fullPath) || pattern.IsMatch(name))))
            {
               _logger.LogDebug("File Ignored: {}", fullPath);

               if (files.Count == 1)
               {
                  return Problem(title: "Unsupported Media Type", detail: "Media file type is unsupported.", statusCode: 415);
               }
               else continue;
            }

            // Is the file a recognized media file?
            if (_mediaLibrary.GetMediaType(fullPath) == MediaContainerType.Unknown)
            {
               _logger.LogDebug("Unrecognized Media File Ignored: {}", fullPath);

               if (files.Count == 1)
               {
                  return Problem(title: "Unsupported Media Type", detail: "Media file type is unsupported.", statusCode: 415);
               }
               else continue;
            }

            try
            {
               using var stream = System.IO.File.Create(fullPath);
               await file.CopyToAsync(stream);

               _logger.LogInformation("File Uploaded: {}", fullPath);
            }
            catch (Exception e)
            {
               _logger.LogWarning("Failed To Copy: {}, Because: {}", file, e.Message);
               _logger.LogDebug("{}", e.ToString());

               if (files.Count == 1)
               {
                  return Problem(title: "Write Error", detail: "Error writing the file to the disk or network location.", statusCode: 403);
               }
               else continue;
            }

            // Process uploaded file...
            // FIXME: Don't rely on or trust the FileName property without validation.

            try
            {
               // Add the file to the MediaLibrary.
               using MediaFile newMediaFile = _mediaLibrary.InsertMediaFile(fullPath);
               result.Add(newMediaFile.Model);
            }
            catch (Exception e)
            {
               _logger.LogWarning("Failed To Insert: {}, Because: {}", file, e.Message);
               _logger.LogDebug("{}", e.ToString());

               if (files.Count == 1)
               {
                  return Problem(detail: e.Message, statusCode: 500);
               }
            }
         }

         return Ok(result);
      }

      [HttpPut]
      [Route("{*path}")]
      [Produces("application/json")]
      public IActionResult Put(List<IFormFile> files, string path = "")
      {
         // Upload a file overwriting existing files with the same name.
         return Ok();
      }

      [HttpGet]
      [Route("history/clear")]
      [Produces("application/json")]
      public IActionResult ClearHistory()
      {
         using IServiceScope scope = _services.CreateScope();
         ISolrIndexService<Models.MediaContainer> solrIndexService = scope.ServiceProvider.GetRequiredService<ISolrIndexService<Models.MediaContainer>>();
         if (solrIndexService.ClearHistory())
         {
            return Ok();
         }
         else
         {
            return Problem(title: "Clearing History Failed", detail: "Failed to clear the viewing history.", statusCode: 500);
         }
      }
   }
}
