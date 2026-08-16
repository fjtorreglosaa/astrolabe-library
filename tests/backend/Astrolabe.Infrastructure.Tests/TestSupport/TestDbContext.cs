using Astrolabe.Application.Abstractions.Events;
using Astrolabe.Domain.Abstractions;
using Astrolabe.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Astrolabe.Infrastructure.Tests.TestSupport;

/// <summary>
/// Builds a context backed by a uniquely named in-memory database, so state never leaks between
/// tests, per SDD_PLIUS_STRATEGY.md section 9.1.
/// </summary>
public static class TestDbContext
{
    public static AstrolabeDbContext Create(IDomainEventDispatcher? dispatcher = null) =>
        new(
            new DbContextOptionsBuilder<AstrolabeDbContext>()
                .UseInMemoryDatabase($"astrolabe-{Guid.NewGuid()}")
                .Options,
            dispatcher ?? new RecordingDomainEventDispatcher());
}

/// <summary>
/// Captures dispatched events instead of publishing them, so a test can assert what an operation
/// raised without wiring MediatR.
/// </summary>
public sealed class RecordingDomainEventDispatcher : IDomainEventDispatcher
{
    private readonly List<IDomainEvent> _dispatched = [];

    public IReadOnlyList<IDomainEvent> Dispatched => _dispatched;

    public Task DispatchAsync(
        IReadOnlyCollection<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        _dispatched.AddRange(domainEvents);
        return Task.CompletedTask;
    }
}
