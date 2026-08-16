using Astrolabe.Domain.Abstractions;

namespace Astrolabe.Application.Tests.TestSupport;

/// <summary>
/// A clock that does not move.
///
/// Shared rather than redeclared per fixture: it was a private nested class in three of them, and a
/// fourth copy is the point at which one of them quietly starts returning a different kind of time.
/// </summary>
public sealed class FixedClock(DateTimeOffset now) : IDateTimeProvider
{
    public DateTimeOffset UtcNow { get; } = now;
}
