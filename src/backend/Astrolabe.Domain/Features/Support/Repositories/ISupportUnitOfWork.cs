using Astrolabe.Domain.Abstractions.Persistence;

namespace Astrolabe.Domain.Features.Support.Repositories;

public interface ISupportUnitOfWork : IUnitOfWork
{
    ITicketRepository Tickets { get; }
}
