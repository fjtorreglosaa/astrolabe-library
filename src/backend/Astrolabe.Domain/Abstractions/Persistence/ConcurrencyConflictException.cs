namespace Astrolabe.Domain.Abstractions.Persistence;

/// <summary>
/// Another request modified the same row between this one reading it and committing.
///
/// <para>
/// Declared in the Domain layer so the Application layer can react to a lost race without knowing
/// which persistence technology detected it. The infrastructure translates its provider-specific
/// exception into this one.
/// </para>
///
/// <para>
/// It is an exception rather than a <c>Result</c> because it is not a business outcome a handler
/// asked about: it surfaces at commit time, after every decision has already been made.
/// </para>
/// </summary>
public sealed class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public ConcurrencyConflictException(string message) : base(message)
    {
    }
}
