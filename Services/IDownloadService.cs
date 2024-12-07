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

namespace Arcadeia.Services
{
   public interface IDownloadService: IHostedService
   {
      Task<string?> DownloadMediaFileAsync(string url, string path, IProgress<string>? progress = null, string template = "%(title)s.%(ext)s", bool overwrite = false);
      Task<string?> GetMediaFileNameAsync(string url, string template = "%(title)s.%(ext)s");
   }
}