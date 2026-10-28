using LeaveFlow.Application.Dtos;
using LeaveFlow.Application.Interfaces.Services;
using LeaveFlow.Host.Hubs;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace LeaveFlow.Host.Services
{
    public class SignalRRealTimeNotifier(IHubContext<NotificationHub> hubContext) : IRealTimeNotifier
    {
        public async Task NotifyUserAsync(Guid userId, NotificationDto notification)
        {
            await hubContext.Clients
                .User(userId.ToString())
                .SendAsync("ReceiveNotification", notification);
        }
    }
}
