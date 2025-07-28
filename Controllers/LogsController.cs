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

using Arcadeia.Providers;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Arcadeia.Controllers
{
   [ApiController]
   [Route("api/[controller]")]
   public partial class LogsController(IConfiguration configuration, ILogger<LogsController> logger, StreamingLoggerProvider provider) : Controller
   {
      private readonly StreamingLoggerProvider provider = provider;
      private readonly IConfigurationRoot configuration = (IConfigurationRoot)configuration;
      private readonly ILogger<LogsController> logger = logger;

      private static async Task WriteAsync(HttpResponse response, string text)
      {
         await response.WriteAsync(text);
         await response.Body.FlushAsync();
      }

      private static async Task LogAsync(HttpResponse response, string message)
      {
         await WriteAsync(response, $"data: {message}\n\n");
      }

      // GET: /api/logs
      [HttpGet]
      public async Task Get()
      {
         Response.ContentType = "text/event-stream";

         // Send the last 500 logs first
         foreach (var message in provider.GetRecentMessages())
         {
            await LogAsync(Response, message);
         }

         var completion = new TaskCompletionSource();
         provider.OnLog += async (message) =>
         {
            await LogAsync(Response, message);
         };

         await completion.Task;
      }
   }
}

