using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Abstractions.Network;
using Astrolabe.Application.Contracts.Support;
using Astrolabe.Application.Shared.Support;
using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Features.Identity.Errors;
using Astrolabe.Domain.Features.Identity.Repositories;
using Astrolabe.Domain.Features.Support.Errors;
using Astrolabe.Domain.Features.Support.Repositories;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Support.Queries.GetTicket;

public sealed class GetTicketQueryHandler(
    ISupportUnitOfWork support,
    IUserRepository users,
    ILibraryScopeProvider scope,
    ILibraryLocationProvider libraries,
    ICurrentUser currentUser) : IQueryHandler<GetTicketQuery, TicketDto>
{
    public async Task<Result<TicketDto>> Handle(
        GetTicketQuery request, CancellationToken cancellationToken)
    {
        if (currentUser is not { UserId: { } callerId, Role: { } role })
        {
            return Result.Failure<TicketDto>(IdentityErrors.InvalidCredentials);
        }

        var ticket = await support.Tickets.GetWithMessagesAsync(request.TicketId, cancellationToken);

        if (ticket is null)
        {
            return Result.Failure<TicketDto>(SupportErrors.TicketNotFound);
        }

        var isTheMember = ticket.MemberId == callerId;

        // BR-SUP-004. Two ways in and no third: the member who opened it, or staff whose scope
        // covers its library. Guessing an identifier gets nobody anywhere.
        if (!isTheMember)
        {
            if (!role.IsStaff())
            {
                return Result.Failure<TicketDto>(SupportErrors.NotYours);
            }

            var reach = await scope.GetCurrentScopeAsync(cancellationToken);

            if (!reach.Covers(ticket.LibraryId))
            {
                return Result.Failure<TicketDto>(SupportErrors.OutOfScope);
            }
        }

        var locations = await libraries.GetAllAsync(cancellationToken);
        var member = await users.GetByIdAsync(ticket.MemberId, cancellationToken);

        return Result.Success(TicketProjection.ToDetail(
            ticket,
            locations.GetValueOrDefault(ticket.LibraryId)?.LibraryName ?? "Unknown library",
            member?.FullName ?? "Unknown",
            isTheMember));
    }
}
