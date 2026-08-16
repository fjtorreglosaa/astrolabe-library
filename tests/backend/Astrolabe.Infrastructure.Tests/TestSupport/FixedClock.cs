using Astrolabe.Domain.Abstractions;

namespace Astrolabe.Infrastructure.Tests.TestSupport;

/// <summary>
/// A clock that does not move.
///
/// Declared here as well as in the application tests, and deliberately so: a test project references
/// only the project under test, per the architecture rules. Sharing it would mean one test assembly
/// depending on another, which is a worse trade than eleven duplicated lines.
/// </summary>
public sealed class FixedClock(DateTimeOffset now) : IDateTimeProvider
{
    public DateTimeOffset UtcNow { get; } = now;
}
