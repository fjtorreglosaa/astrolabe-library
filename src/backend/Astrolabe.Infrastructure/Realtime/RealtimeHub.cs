using System.Security.Claims;
using Astrolabe.Domain.Features.Identity.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Astrolabe.Infrastructure.Realtime;

/// <summary>
/// The one hub. Clients connect, are placed in the groups their token entitles them to, and listen.
/// </summary>
/// <remarks>
/// <para>
/// <b>There are no callable methods.</b> Everything a client could ask for it can already ask for
/// over HTTP, where the request passes through a controller, a policy, a handler and the same
/// validation as every other caller. A hub method would be a second front door into the same data
/// with none of that behind it, so this one is deliberately write-only from the server's side.
/// </para>
/// <para>
/// Membership of a group is decided <b>here, from the validated token</b>, and never from anything
/// the client sends. A <c>Subscribe(userId)</c> method would let any signed-in member join any other
/// member's group and watch their fines arrive — the classic SignalR hole, and the reason this class
/// has no parameters to trust.
/// </para>
/// </remarks>
[Authorize]
public sealed class RealtimeHub(ILogger<RealtimeHub> logger) : Hub<IRealtimeClient>
{
    public override async Task OnConnectedAsync()
    {
        var user = Context.User;

        // Authorize has already run, so this is a guard against a token that somehow validated
        // without the claim rather than an expected path.
        if (!Guid.TryParse(user?.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            logger.LogWarning("A connection was authorized without a usable subject claim. Refusing it.");
            Context.Abort();
            return;
        }

        var role = user?.FindFirstValue(ClaimTypes.Role);

        // Staff and members are disjoint audiences: a member group for staff would only ever receive
        // events about a plan, loans and fines that staff do not have.
        if (role == nameof(UserRole.Admin) || role == nameof(UserRole.SuperAdmin))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, RealtimeGroups.Staff);
        }
        else
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, RealtimeGroups.ForMember(userId));
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Groups are cleaned up with the connection, so there is nothing to undo here. Logged
        // because a rising rate of faulted disconnects is the first sign of a proxy that is closing
        // long-lived sockets.
        if (exception is not null)
        {
            logger.LogInformation(exception, "A realtime connection ended with an error.");
        }

        await base.OnDisconnectedAsync(exception);
    }
}
