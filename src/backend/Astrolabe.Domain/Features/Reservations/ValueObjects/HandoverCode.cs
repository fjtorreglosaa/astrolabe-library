namespace Astrolabe.Domain.Features.Reservations.ValueObjects;

/// <summary>
/// The code the courier or librarian reads out and the member types back. Implements BR-RSV-013 and
/// BR-RSV-014.
///
/// <para>
/// Derived from the reservation identifier, exactly as the prototype's <c>pickupCode</c> does, and
/// deliberately <b>not</b> a secret. It is proof that two people standing together completed a
/// handover: the courier says it aloud. Making it unguessable would buy nothing, because whoever
/// guessed it would still have to physically take the book — and the library's check-in is the fact
/// that actually settles the return.
/// </para>
/// </summary>
public sealed record HandoverCode
{
    private const string Prefix = "PU-";

    private HandoverCode(string value) => Value = value;

    public string Value { get; }

    public static HandoverCode For(Guid reservationId)
    {
        var text = reservationId.ToString();
        var hash = 0;

        // The prototype's own hash, kept digit-for-digit so a code shown in the mockup is the code
        // the system produces.
        foreach (var character in text)
        {
            hash = (hash * 31 + character) % 9000;
        }

        return new HandoverCode($"{Prefix}{1000 + hash}");
    }

    /// <summary>
    /// Compares what the member typed. Trimmed and case-insensitive on the <em>input</em> only: the
    /// member is copying something read aloud, and rejecting a trailing space would be theatre.
    /// </summary>
    public bool Matches(string? typed) =>
        !string.IsNullOrWhiteSpace(typed)
        && string.Equals(typed.Trim(), Value, StringComparison.OrdinalIgnoreCase);

    public override string ToString() => Value;
}
