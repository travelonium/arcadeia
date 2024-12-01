using MediaCurator.Hubs;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace MediaCurator.Services
{
    public class NotificationService
    {
        private readonly IHubContext<SignalRHub> _hubContext;

        public NotificationService(IHubContext<SignalRHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task ShowUpdateProgressAsync(string uuid, string title, string item, int index, int total)
        {
            await _hubContext.Clients.All.SendAsync("ShowUpdateProgress", uuid, title, item, index, total);
        }

        public void ShowUpdateProgress(string uuid, string title, string item, int index, int total)
        {
            Task.Run(() => ShowUpdateProgressAsync(uuid, title, item, index, total));
        }

        public async Task ShowScanProgressAsync(string uuid, string title, string path, string item, int index, int total)
        {
            await _hubContext.Clients.All.SendAsync("ShowScanProgress", uuid, title, path, item, index, total);
        }

        public void ShowScanProgress(string uuid, string title, string path, string item, int index, int total)
        {
            Task.Run(() => ShowScanProgressAsync(uuid, title, path, item, index, total));
        }

        public async Task RefreshAsync(string path)
        {
            await _hubContext.Clients.All.SendAsync("Refresh", path);
        }

        public void Refresh(string path)
        {
            Task.Run(() => RefreshAsync(path));
        }
    }
}