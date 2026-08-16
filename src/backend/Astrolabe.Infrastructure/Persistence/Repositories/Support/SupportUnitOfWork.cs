using Astrolabe.Domain.Features.Support.Repositories;

namespace Astrolabe.Infrastructure.Persistence.Repositories.Support;

public sealed class SupportUnitOfWork(AstrolabeDbContext context, ITicketRepository tickets)
    : UnitOfWorkBase(context), ISupportUnitOfWork
{
    public ITicketRepository Tickets { get; } = tickets;
}
