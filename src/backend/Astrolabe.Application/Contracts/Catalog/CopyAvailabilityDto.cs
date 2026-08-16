namespace Astrolabe.Application.Contracts.Catalog;

/// <summary>
/// One branch's holding, with the caller's verdict for it. The detail panel lists every branch and
/// its own reason, which is why the per-copy verdict travels rather than only the book-level badge.
/// </summary>
public sealed record CopyAvailabilityDto(
    Guid LibraryId,
    string LibraryName,
    string CityName,
    int AvailableCount,
    int TotalCount,
    bool CanReserve,
    string? Reason);
