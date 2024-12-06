using MediaCurator.Hubs;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace MediaCurator.Services
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