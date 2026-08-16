namespace Astrolabe.Domain.Features.Reservations.Enums;

/// <summary>
/// How the copy goes back. Both require the same handover code: the difference is who reads it out.
/// </summary>
public enum ReturnMethod
{
    /// <summary>A courier collects it at the member's door and reads out the code.</summary>
    CourierPickup = 0,

    /// <summary>The member hands it to the desk and the librarian reads out the code.</summary>
    LibraryDropOff = 1
}
