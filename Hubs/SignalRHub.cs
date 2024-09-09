using Microsoft.AspNetCore.SignalR;

namespace MediaCurator.Hubs
{
   public class SignalRHub : Hub
   {
      public async Task SendMessage(string user, string message)
      {
         await Clients.All.SendAsync("ReceiveMessage", user, message);
      }
   }
}
