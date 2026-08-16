namespace Astrolabe.Presentation.Contracts.Support;

/// <summary>
/// A message. There is no author field: whether this is a member or an agent is decided from the
/// caller's role, so a member cannot post as staff.
/// </summary>
public sealed record ReplyToTicketRequest(string Text);
