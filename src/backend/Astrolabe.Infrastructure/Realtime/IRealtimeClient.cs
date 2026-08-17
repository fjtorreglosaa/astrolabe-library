using Astrolabe.Application.Contracts.Realtime;

namespace Astrolabe.Infrastructure.Realtime;

/// <summary>
/// The methods the server may call on a connected browser.
/// </summary>
/// <remarks>
/// A strongly typed hub client. Without it the method name is a magic string at every call site and
/// a typo is a message that is sent, accepted and silently never delivered — SignalR has no way to
/// know the client was not listening for <c>Chagned</c>. Here the compiler knows.
/// </remarks>
public interface IRealtimeClient
{
    /// <summary>
    /// Something the recipient can see has changed. The client decides what to refetch.
    /// </summary>
    Task Changed(RealtimeEvent @event);
}
