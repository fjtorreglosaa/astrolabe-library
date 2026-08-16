using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Abstractions.Network;
using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Features.Identity.Errors;
using Astrolabe.Domain.Features.Identity.Repositories;
using Astrolabe.Domain.Features.Support.Enums;
using Astrolabe.Domain.Features.Support.Errors;
using Astrolabe.Domain.Features.Support.Repositories;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Support.Commands.ReplyToTicket;

public sealed class ReplyToTicketCommandHandler(
    ISupportUnitOfWork support,
    IUserRepository users,
    ILibraryScopeProvider scope,
    ICurrentUser currentUser,
    IDateTimeProvider clock) : ICommandHandler<ReplyToTicketCommand>
{
    public async Task<Result> Handle(
        ReplyToTicketCommand request, CancellationToken cancellationToken)
    {
        if (currentUser is not { UserId: { } callerId, Role: { } role })
        {
            return Result.Failure(IdentityErrors.InvalidCredentials);
        }

        var ticket = await support.Tickets.GetWithMessagesAsync(request.TicketId, cancellationToken);

        if (ticket is null)
        {
            return Result.Failure(SupportErrors.TicketNotFound);
        }

        // Decided from the caller's role, never from the payload — otherwise a member could post a
        // message that reads as staff, and the conversation would stop being evidence of anything.
        var author = role.IsStaff() ? TicketAuthor.Agent : TicketAuthor.Member;

        if (author is TicketAuthor.Member)
        {
            // BR-SUP-004. A member may only reply to their own.
            if (ticket.MemberId != callerId)
            {
                return Result.Failure(SupportErrors.NotYours);
            }
        }
        else
        {
            // BR-SUP-010. Staff act only within their libraries.
            var reach = await scope.GetCurrentScopeAsync(cancellationToken);

            if (!reach.Covers(ticket.LibraryId))
            {
                return Result.Failure(SupportErrors.OutOfScope);
            }
        }

        var caller = await users.GetByIdAsync(callerId, cancellationToken);

        // BR-SUP-011 lives in the aggregate, so a resolved ticket refuses here whoever is writing.
        // Raises TicketAnswered when an agent writes, which notifications reacts to.
        var result = ticket.Reply(
            callerId, author, caller?.FullName ?? "Unknown", request.Text, clock.UtcNow);

        if (result.IsFailure)
        {
            return result;
        }

        await support.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
