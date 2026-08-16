namespace Astrolabe.Presentation.Contracts.Network;

/// <summary>
/// The complete set an administrator should hold, not a delta. An empty list is meaningful and
/// allowed: BR-NET-010 describes an administrator who can sign in and see nothing.
/// </summary>
public sealed record AssignLibrariesRequest(IReadOnlyList<Guid> LibraryIds);
