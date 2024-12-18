/*
 *  Copyright © 2024 Travelonium AB
 *
 *  This file is part of Arcadeia.
 *
 *  Arcadeia is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU Affero General Public License as published
 *  by the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  Arcadeia is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU Affero General Public License for more details.
 *
 *  You should have received a copy of the GNU Affero General Public License
 *  along with Arcadeia. If not, see <https://www.gnu.org/licenses/>.
 *
 */

using System.Net;
using System.Text.Json;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc;
using Arcadeia.Configuration;
using Arcadeia.Services;
using Arcadeia.Solr;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Arcadeia.Controllers
{
   [ApiController]
   [Route("api/[controller]")]
   public class LibraryController(IServiceProvider services,
                                  IOptionsMonitor<Settings> settings,
                                  IMediaLibrary mediaLibrary,
                                  IThumbnailsDatabase thumbnailsDatabase,
                                  ILogger<MediaContainer> logger) : Controller
   {
      private readonly IServiceProvider _services = services;
      private readonly IMediaLibrary _mediaLibrary = mediaLibrary;
      private readonly IOptionsMonitor<Settings> _settings = settings;
      private readonly IThumbnailsDatabase _thumbnailsDatabase = thumbnailsDatabase;
      private readonly ILogger<MediaContainer> _logger = logger;

      private static async Task WriteAsync(HttpResponse response, string text)
      {
         await response.WriteAsync(text);
         await response.Body.FlushAsync();
      }

      private static string RemoveEmojis(string fileName)
      {
         var result = new System.Text.StringBuilder();

         foreach (var character in fileName)
         {
            // Only include characters within the Basic Multilingual Plane (BMP)
            var category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category != UnicodeCategory.OtherSymbol && !char.IsSurrogate(character))
            {
               result.Append(character);
            }
         }

         return result.ToString();
      }

      private static string RemoveSpaces(string fileName)
      {
         // Normalize spaces in the file name
         fileName = Regex.Replace(fileName, @"\s+", " ");

         // Split into components (name and extension)
         string name = Path.GetFileNameWithoutExtension(fileName);
         string extension = Path.GetExtension(fileName);

         // Trim each component and join back
         return $"{name.Trim()}{extension.Trim()}";
      }

      private static string GetUniqueFileName(string path, string fileName, bool absolute = true)
      {
         fileName = RemoveSpaces(RemoveEmojis(fileName));

         if (!System.IO.File.Exists(Path.Combine(path, fileName)))
         {
            return absolute ? Path.Combine(path, fileName) : fileName;
         }

         // Extract the file extension and base name
         var extension = Path.GetExtension(fileName);
         var name = Path.GetFileNameWithoutExtension(fileName);

         // Regex pattern to identify existing index suffix
         Regex pattern = new(@"(.*)(\s+\(\d+\))");
         var match = pattern.Match(name);

         if (match.Success)
         {
            // Remove the existing index suffix if present
            name = match.Groups[1].Value;
         }

         // Find a unique file name by incrementing the index
         for (int i = 1; i < Int32.MaxValue; i++)
         {
            if (!System.IO.File.Exists(Path.Combine(path, $"{name}({i}){extension}")) &&
                !System.IO.File.Exists(Path.Combine(path, $"{name} ({i}){extension}")))
            {
               // Return the unique file name
               return absolute ? Path.Combine(path, $"{name} ({i}){extension}") : $"{name} ({i}){extension}";
            }
         }

         throw new InvalidOperationException("Unable to generate a unique file name.");
      }

      private bool IsPathAllowed(string path)
      {
         string fullPath = Path.GetFullPath(path);
         return _settings.CurrentValue.Scanner.Folders.Concat(_settings.CurrentValue.Scanner.WatchedFolders).Any(folder =>
         {
            string folderPath = Path.GetFullPath(folder);
            if (fullPath.Length < folderPath.Length) return false;
            return fullPath.StartsWith(folderPath, StringComparison.OrdinalIgnoreCase) &&
                   (fullPath.Length == folderPath.Length || fullPath[folderPath.Length] == Path.DirectorySeparatorChar);
         });
      }

      // PATCH: /api/library/{path}
      [HttpPatch]
      [Route("{*path}")]
      [Produces("application/json")]
      public IActionResult Patch([FromBody] Models.MediaContainer modified, string path = "")
      {
         if (_settings.CurrentValue.Security.Library.ReadOnly)
         {
            return Problem(title: "Permission Denied", detail: "The library is read-only.", statusCode: 403);
         }

         path = Platform.Separator.Path + path;

         if (!IsPathAllowed(path)) {
            return Problem(title: "Bad Request", detail: "The path is invalid or inaccessible.", statusCode: 400);
         }

         try
         {
            if ((!System.IO.Directory.Exists(path) && !System.IO.File.Exists(path)) || string.IsNullOrEmpty(modified.Id) || string.IsNullOrEmpty(modified.Type))
            {
               return NotFound(new
               {
                  message = "File or folder not found."
               });
            }

            var type = modified.Type.ToEnum<MediaContainerType>().ToType();

            if (type == null) return StatusCode((int)HttpStatusCode.InternalServerError, new
            {
               message = "Failed to instantiate the MediaContainer instance."
            });

            // Create the parent container of the right type.
            using IMediaContainer? mediaContainer = Activator.CreateInstance(type, _logger, _services, _settings, _thumbnailsDatabase, _mediaLibrary, modified.Id, null, null) as IMediaContainer;

            if (mediaContainer == null) return StatusCode((int)HttpStatusCode.InternalServerError, new
            {
               message = "Failed to instantiate the MediaContainer instance."
            });

            // Reset the Views to its currently stored value as the UI is not allowed to update it.
            modified.Views = mediaContainer.Model.Views;

            // Update the container with the modified version.
            mediaContainer.Model = modified;

            return Ok(mediaContainer?.Model);
         }
         catch (Exception ex)
         {
            return StatusCode((int)HttpStatusCode.InternalServerError, new
            {
               message = ex.Message
            });
         }
      }

      // POST: /api/library/{path}
      /// <summary>
      /// Upload a file choosing whether to overwrite it or create one with an index if already exists.
      /// </summary>
      /// <param name="files">List of files to upload.</param>
      /// <param name="path">The directory where the files shall be uploaded to.</param>
      /// <param name="overwrite">If true, the file shall be overwritten.</param>
      ///  <param name="duplicate">If true, the file will be created with an index if one exists with the same name.</param>
      /// <returns></returns>
      [HttpPost]
      [Route("{*path}")]
      [Produces("application/json")]
      [RequestSizeLimit(10L * 1024 * 1024 * 1024)]
      [RequestFormLimits(MultipartBodyLengthLimit = 10L * 1024 * 1024 * 1024)]
      public async Task<IActionResult> Post(List<IFormFile> files, string path = "", [FromQuery] bool overwrite = false, [FromQuery] bool duplicate = false)
      {
         if (_settings.CurrentValue.Security.Library.ReadOnly)
         {
            return Problem(title: "Permission Denied", detail: "The library is read-only.", statusCode: 403);
         }

         path = Platform.Separator.Path + path;

         if (!IsPathAllowed(path)) {
            return Problem(title: "Bad Request", detail: "The path is invalid or inaccessible.", statusCode: 400);
         }

         long size = files.Sum(file => file.Length);
         var result = new List<Models.MediaContainer>();

         if (overwrite && duplicate)
         {
            return Problem(title: "Bad Request", detail: "Either overwrite or duplicate must be false.", statusCode: 400);
         }

         if (!System.IO.Directory.Exists(path))
         {
            try
            {
               System.IO.Directory.CreateDirectory(path);
            }
            catch (Exception ex)
            {
               _logger.LogWarning("Failed To Create Path: {}, Because: {}", path, ex.Message);
               _logger.LogDebug("{}", ex.ToString());

               return Problem(title: "Path Creation Failed", detail: "Failed to create the destination path.", statusCode: 500);
            }
         }

         foreach (var file in files)
         {
            var name = Path.GetFileName(file.FileName);
            var fullPath = Path.Combine(path, file.FileName);
            var patterns = _settings.CurrentValue.Scanner.IgnoredPatterns.Select(pattern => new Regex(pattern, RegexOptions.IgnoreCase)).ToList<Regex>();

            if (System.IO.File.Exists(fullPath))
            {
               if (overwrite)
               {
                  // It shall be overwritten then.
               }
               else if (duplicate)
               {
                  fullPath = GetUniqueFileName(path, file.FileName);
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
            if ((file.Length <= 0) || patterns.Any(pattern => pattern.IsMatch(fullPath) || pattern.IsMatch(name)))
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
            catch (Exception ex)
            {
               _logger.LogWarning("Failed To Copy: {}, Because: {}", file, ex.Message);
               _logger.LogDebug("{}", ex.ToString());

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
               using MediaFile? newMediaFile = _mediaLibrary.InsertMediaFile(fullPath);

               if (newMediaFile != null) result.Add(newMediaFile.Model);
               else {
                  _logger.LogWarning("Failed To Insert: {}", file);
               }
            }
            catch (Exception ex)
            {
               _logger.LogWarning("Failed To Insert: {}, Because: {}", file, ex.Message);
               _logger.LogDebug("{}", ex.ToString());

               if (files.Count == 1)
               {
                  return Problem(detail: ex.Message, statusCode: 500);
               }
            }
         }

         return Ok(result);
      }

      // PUT: /api/library/{path}
      [HttpPut]
      [Route("{*path}")]
      [Produces("application/json")]
      [RequestSizeLimit(10L * 1024 * 1024 * 1024)]
      [RequestFormLimits(MultipartBodyLengthLimit = 10L * 1024 * 1024 * 1024)]
      public async Task<IActionResult> Put(List<IFormFile> files, string path = "")
      {
         if (_settings.CurrentValue.Security.Library.ReadOnly)
         {
            return Problem(title: "Permission Denied", detail: "The library is read-only.", statusCode: 403);
         }

         return await Post(files, path, overwrite: true, duplicate: false);
      }

      // GET: /api/library/upload
      [HttpGet]
      [Route("upload")]
      public async Task Upload([FromQuery] String url, [FromQuery] string path, [FromQuery] bool overwrite = false, [FromQuery] bool duplicate = false)
      {
         if (_settings.CurrentValue.Security.Library.ReadOnly)
         {
            Response.StatusCode = 403;
            await WriteAsync(Response, "Error: The library is read-only.\n");
            return;
         }

         if (string.IsNullOrWhiteSpace(url))
         {
            await WriteAsync(Response, "Error: URL is required.\n");
            return;
         }

         if (!IsPathAllowed(path)) {
            await WriteAsync(Response, "Error: The destination path is inaccessible.\n");
            return;
         }

         IDownloadService? mediaFileDownloadService = _services.GetService<IDownloadService>();

         if (mediaFileDownloadService == null)
         {
            await WriteAsync(Response, "Error: Download service is unavailable.\n");
            return;
         }

         Response.ContentType = "text/event-stream";

         OrderedAsyncProgress<string> downloadingProgress = new(async value => await WriteAsync(Response, value));

         Progress<float> processingProgress = new(async value => await WriteAsync(Response, $"Processing: {value:0.000}\n"));

         try
         {
            string? fileName = await mediaFileDownloadService.GetMediaFileNameAsync(url);

            if (fileName != null)
            {
               if (overwrite)
               {
                  // It shall be overwritten then.
               }
               else if (duplicate)
               {
                  fileName = GetUniqueFileName(path, fileName, false);
               }

               await WriteAsync(Response, $"Name: {fileName}\n");
            }
            else
            {
               fileName = "%(title)s.%(ext)s";
            }

            string? file = await mediaFileDownloadService.DownloadMediaFileAsync(url, path, downloadingProgress, fileName, overwrite);

            if (file != null)
            {
               await WriteAsync(Response, $"Processing: {file}\n");
               using MediaFile? mediaFile = _mediaLibrary.InsertMediaFile(file, processingProgress);
               if (mediaFile != null)
               {
                  string mediaFileJson = JsonSerializer.Serialize(mediaFile.Model);
                  await WriteAsync(Response, $"Result: {mediaFileJson}\n");
               }
               else
               {
                  await WriteAsync(Response, "Error: Failed to process the media file.\n");
               }
            }
            else
            {
               await WriteAsync(Response, "Error: Media file downloading failed.\n");
            }
         }
         catch (DownloadService.FileAlreadyDownloadedException ex)
         {
            await WriteAsync(Response, $"Error: {ex.Message}\n");
            _logger.LogError("Failed To Download URL: {}, Because: {}", url, ex.Message);
            _logger.LogDebug("{Exception}", ex.ToString());
         }
         catch (System.Exception ex)
         {
            await WriteAsync(Response, $"Error: {ex.Message}\n");
            _logger.LogError("Failed To Download URL: {}, Because: {}", url, ex.Message);
            _logger.LogDebug("{Exception}", ex.ToString());
            throw;
         }
      }

      // GET: /api/library/clear/history
      [HttpGet]
      [Route("clear/history")]
      [Produces("application/json")]
      public IActionResult ClearHistory()
      {
         if (_settings.CurrentValue.Security.Library.ReadOnly)
         {
            return Problem(title: "Permission Denied", detail: "The library is read-only.", statusCode: 403);
         }

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
