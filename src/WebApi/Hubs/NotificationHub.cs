using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace KingStore.Infrastructure.RealTime;

[Authorize]
public class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userRole = Context.User?.FindFirst(ClaimTypes.Role)?.Value 
                    ?? Context.User?.FindFirst("role")?.Value;


        if (userRole == "Admin")
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "Admin");
        }

        await base.OnConnectedAsync();
    }
    
}