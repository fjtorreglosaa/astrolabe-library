using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Abstractions.Network;
using Astrolabe.Application.Contracts.Support;
using Astrolabe.Application.Shared.Support;
using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Identity.Errors;
using Astrolabe.Domain.Features.Identity.Repositories;
using Astrolabe.Domain.Features.Support.Entities;
using Astrolabe.Domain.Features.Support.Repositories;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Support.Commands.OpenTicket;

public sealed class OpenTicketCommandHandler(
    ISupportUnitOfWork support,
    IUserRepository users,
    ILibraryLocationProvider libraries,
    ICurrentUser currentUser,
    IDateTimeProvider clock) : ICommandHandler<OpenTicketCommand, TicketDto>
{
    public async Task<Result<TicketDto>> Handle(
        OpenTicketCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } memberId)
        {
            return Result.Failure<TicketDto>(IdentityErrors.InvalidCredentials);
        }

        var member = await users.GetByIdAsync(memberId, cancellationToken);

        if (member is null)
        {
            return Result.Failure<TicketDto>(IdentityErrors.AccountNotFound);
        }

        var locations = await libraries.GetAllAsync(cancellationToken);
        var location = locations.GetValueOrDefault(request.LibraryId);

        if (location is null)
        {
            return Result.Failure<TicketDto>(
                Domain.Features.Network.Errors.NetworkErrors.LibraryNotFound);
        }

        // Sequential rather than random. A member reads this aloud on the phone, which is the whole
        // reason it exists alongside the identifier.
        var next = await support.Tickets.NextReferenceNumberAsync(cancellationToken);
        var now = clock.UtcNow;

        var ticket = Ticket.Open(
            $"TCK-{next}", memberId, request.Category, request.LibraryId,
            request.Subject, request.Body, member.FullName, now);

        if (ticket.IsFailure)
        {
            return Result.Failure<TicketDto>(ticket.Error);
        }

        await support.Tickets.AddAsync(ticket.Value, cancellationToken);
        await support.SaveChangesAsync(cancellationToken);

        return Result.Success(TicketProjection.ToDetail(
            ticket.Value, location.LibraryName, member.FullName, viewerIsTheMember: true));
    }
}
