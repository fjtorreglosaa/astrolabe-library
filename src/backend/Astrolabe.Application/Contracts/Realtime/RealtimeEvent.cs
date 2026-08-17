namespace Astrolabe.Application.Contracts.Realtime;

/// <summary>
/// What a connected client is told when something it cares about has changed.
/// </summary>
/// <remarks>
/// <para>
/// <b>This carries a signal, not state.</b> The name says what happened and the identifier says
/// which thing it happened to; the client then refetches through the same authorized endpoint it
/// would have used anyway. Pushing the changed entity instead would make the hub a second read API —
/// one with its own copy of every projection and, worse, its own copy of every authorization
/// decision. A member's fine and a librarian's view of that fine are different documents, and only
/// the query handlers know the difference.
/// </para>
/// <para>
/// It also keeps the transport honest about what it is. A dropped frame or a reconnect costs a
/// refetch, never a wrong number on screen, because nothing on screen was ever sourced from here.
/// </para>
/// </remarks>
/// <param name="Name">One of <see cref="RealtimeEventNames"/>. The client maps it to what to refetch.</param>
/// <param name="OccurredAt">
/// When the change happened, not when it was sent. A client that reconnects can tell a replayed
/// event from a fresh one.
/// </param>
/// <param name="SubjectId">
/// The thing that changed — a reservation, a fine, a ticket — when naming it lets the client narrow
/// its refetch to one row. Null when the event is about a collection as a whole.
/// </param>
public sealed record RealtimeEvent(string Name, DateTimeOffset OccurredAt, Guid? SubjectId = null);
