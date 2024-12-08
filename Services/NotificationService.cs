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

using Arcadeia.Hubs;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace Arcadeia.Services
{
    public class NotificationService(IHubContext<SignalRHub> hubContext)
    {
        private readonly IHubContext<SignalRHub> _hubContext = hubContext;

        public async Task ShowUpdateProgressAsync(string uuid, string title, string item, int index, int total)
        {
            await _hubContext.Clients.All.SendAsync("ShowUpdateProgress", uuid, title, item, index, total);
        }

        public async Task UpdateCancelledAsync(string uuid, string title)
        {
            await _hubContext.Clients.All.SendAsync("UpdateCancelled", uuid, title);
        }

        public async Task ShowScanProgressAsync(string uuid, string title, string path, string item, int index, int total)
        {
            await _hubContext.Clients.All.SendAsync("ShowScanProgress", uuid, title, path, item, index, total);
        }

        public async Task ScanCancelledAsync(string uuid, string title, string path)
        {
            await _hubContext.Clients.All.SendAsync("ScanCancelled", uuid, title, path);
        }

        public async Task RefreshAsync(string path)
        {
            await _hubContext.Clients.All.SendAsync("Refresh", path);
        }
    }
}