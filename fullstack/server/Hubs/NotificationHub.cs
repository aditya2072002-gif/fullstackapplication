using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace fullstackapplication.Server.Hubs;

[Authorize]
public sealed class NotificationHub : Hub
{
    public static string GetUserGroup(int accountId) => $"user:{accountId}";

    public override async Task OnConnectedAsync()
    {
        var accountId = Context.User?.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(accountId, out var parsedAccountId))
        {
            Context.Abort();
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GetUserGroup(parsedAccountId));
        await base.OnConnectedAsync();
    }
}
