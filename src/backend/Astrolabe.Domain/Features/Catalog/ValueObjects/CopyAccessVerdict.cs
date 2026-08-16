using Astrolabe.Domain.Features.Catalog.Enums;

namespace Astrolabe.Domain.Features.Catalog.ValueObjects;

/// <summary>The decision for one member and one copy: reservable, or not and why.</summary>
public sealed record CopyAccessVerdict(Guid LibraryId, bool CanReserve, CopyRejection? Reason)
{
    public static CopyAccessVerdict Allowed(Guid libraryId) => new(libraryId, true, null);

    public static CopyAccessVerdict Refused(Guid libraryId, CopyRejection reason) =>
        new(libraryId, false, reason);
}
