using Astrolabe.Domain.Abstractions;

namespace Astrolabe.Infrastructure.Time;

/// <summary>Reads the real clock. Always UTC, per BR-GLOBAL-002.</summary>
public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
