using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace LeaveFlow.Host.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
    }
}
