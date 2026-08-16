namespace Astrolabe.Domain.Features.Catalog.Enums;

/// <summary>Why a book was withdrawn for repair. Required by BR-CAT-023.</summary>
public enum RepairReason
{
    DamagedSpine = 0,
    WaterDamage = 1,
    MissingPages = 2,
    Rebinding = 3,
    CoverReplacement = 4,
    Other = 5
}
