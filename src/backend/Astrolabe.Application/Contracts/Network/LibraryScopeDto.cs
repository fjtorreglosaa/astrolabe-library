namespace Astrolabe.Application.Contracts.Network;

/// <summary>
/// What the caller may act on. <c>IsUnrestricted</c> is separate from an empty list on purpose: a
/// super administrator reaches everything, an unassigned administrator reaches nothing, and the
/// interface must tell those apart.
/// </summary>
public sealed record LibraryScopeDto(bool IsUnrestricted, IReadOnlyList<Guid> LibraryIds);
