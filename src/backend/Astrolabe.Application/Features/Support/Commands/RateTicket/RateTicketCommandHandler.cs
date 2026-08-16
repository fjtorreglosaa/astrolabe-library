using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Identity.Errors;
using Astrolabe.Domain.Features.Support.Errors;
using Astrolabe.Domain.Features.Support.Repositories;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Support.Commands.RateTicket;

public sealed class RateTicketCommandHandler(
    ISupportUnitOfWork support,
    ICurrentUser currentUser,
    IDateTimeProvider clock) : ICommandHandler<RateTicketCommand>
{
    public async Task<Result> Handle(
        RateTicketCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } memberId)
        {
            return Result.Failure(IdentityErrors.InvalidCredentials);
        }

        var ticket = await support.Tickets.GetByIdAsync(request.TicketId, cancellationToken);

        if (ticket is null)
        {
            return Result.Failure(SupportErrors.TicketNotFound);
        }

        // BR-SUP-005. Only their own, and staff never rate their own work — there is no branch here
        // that lets them, by design rather than by omission.
        if (ticket.MemberId != memberId)
        {
            return Result.Failure(SupportErrors.NotYours);
        }

        var result = ticket.Rate(request.Stars, request.Review, clock.UtcNow);

        if (result.IsFailure)
        {
            return result;
        }

        await support.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
