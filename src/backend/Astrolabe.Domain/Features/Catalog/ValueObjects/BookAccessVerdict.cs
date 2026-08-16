using Astrolabe.Domain.Features.Catalog.Enums;

namespace Astrolabe.Domain.Features.Catalog.ValueObjects;

/// <summary>
/// The decision for one member and one book, with the per-copy verdicts that produced it.
///
/// Both halves are returned together because the interface needs both: the card shows the single
/// badge, and the detail panel lists every branch with its own reason.
/// </summary>
public sealed record BookAccessVerdict(
    bool CanReserve,
    BookRejection? Badge,
    IReadOnlyList<CopyAccessVerdict> Copies);
