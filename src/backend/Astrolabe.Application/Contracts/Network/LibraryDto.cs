namespace Astrolabe.Application.Contracts.Network;

/// <summary>A library branch.</summary>
public sealed record LibraryDto(
    Guid Id,
    Guid CityId,
    string Name,
    bool IsActive,
    bool IsCityHomeLibrary);
