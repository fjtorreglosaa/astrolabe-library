namespace Astrolabe.Domain.Features.Catalog.Enums;

/// <summary>Why a book left the collection. Required by BR-CAT-024.</summary>
public enum RemovalReason
{
    Donated = 0,
    DamagedBeyondRepair = 1,
    LostByMember = 2,
    WithdrawnFromCollection = 3,
    Other = 4
}
