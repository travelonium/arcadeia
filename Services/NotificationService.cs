using MediaCurator.Hubs;
using Microsoft.AspNetCore.SignalR;

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
            ShowUpdateProgressAsync(uuid, title, item, index, total).Wait();
        }

        public async Task ShowScanProgressAsync(string uuid, string title, string path, string item, int index, int total)
        {
            await _hubContext.Clients.All.SendAsync("ShowScanProgress", uuid, title, path, item, index, total);
        }

        public void ShowScanProgress(string uuid, string title, string path, string item, int index, int total)
        {
            ShowScanProgressAsync(uuid, title, path, item, index, total).Wait();
        }
    }
}
