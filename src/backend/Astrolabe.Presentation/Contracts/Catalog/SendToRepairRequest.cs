using Astrolabe.Domain.Features.Catalog.Enums;

namespace Astrolabe.Presentation.Contracts.Catalog;

/// <summary>The body of a repair. BR-CAT-023 makes the reason mandatory.</summary>
public sealed record SendToRepairRequest(
    RepairReason Reason,
    DateTimeOffset? ExpectedBack,
    string? Notes);
