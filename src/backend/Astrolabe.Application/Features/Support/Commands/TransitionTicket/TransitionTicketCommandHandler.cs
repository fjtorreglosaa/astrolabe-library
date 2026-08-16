using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Abstractions.Network;
using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Audit.Entities;
using Astrolabe.Domain.Features.Audit.Repositories;
using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Features.Identity.Repositories;
using Astrolabe.Domain.Features.Network.Errors;
using Astrolabe.Domain.Features.Support.Errors;
using Astrolabe.Domain.Features.Support.Repositories;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Support.Commands.TransitionTicket;

public sealed class TransitionTicketCommandHandler(
    ISupportUnitOfWork support,
    IAuditUnitOfWork audit,
    IUserRepository users,
    ILibraryScopeProvider scope,
    ICurrentUser currentUser,
    IDateTimeProvider clock) : ICommandHandler<TransitionTicketCommand>
{
    public async Task<Result> Handle(
        TransitionTicketCommand request, CancellationToken cancellationToken)
    {
        if (currentUser is not { UserId: { } actorId, Role: { } role })
        {
            return Result.Failure(NetworkErrors.StaffRequired);
        }

        if (!role.IsStaff())
        {
            return Result.Failure(NetworkErrors.StaffRequired);
        }

        var ticket = await support.Tickets.GetWithMessagesAsync(request.TicketId, cancellationToken);

        if (ticket is null)
        {
            return Result.Failure(SupportErrors.TicketNotFound);
        }

        // BR-SUP-010, checked before any transition rather than after. Scope is about which queue a
        // ticket belongs to, and acting on somebody else's is not a mistake to discover afterwards.
        var reach = await scope.GetCurrentScopeAsync(cancellationToken);

        if (!reach.Covers(ticket.LibraryId))
        {
            return Result.Failure(SupportErrors.OutOfScope);
        }

        var now = clock.UtcNow;

        var result = request.Transition switch
        {
            TicketTransition.Assign => await AssignAsync(ticket, actorId, now, cancellationToken),
            TicketTransition.Resolve => ticket.Resolve(now),
            _ => ticket.Reopen(now)
        };

        if (result.IsFailure)
        {
            return result;
        }

        // In the same transaction as the change, like every other administrative act in this system.
        await audit.Entries.AddAsync(
            AuditEntry.Record(
                $"support.ticket_{request.Transition.ToString().ToLowerInvariant()}", now,
                actorUserId: actorId, subjectUserId: ticket.MemberId,
                detail: ticket.Reference),
            cancellationToken);

        await support.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    /// <summary>
    /// BR-SUP-003. The assignee is the caller: a queue where staff assign tickets to each other is a
    /// queue where nothing gets picked up, and the prototype has one action, not two.
    /// </summary>
    private async Task<Result> AssignAsync(
        Domain.Features.Support.Entities.Ticket ticket,
        Guid actorId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var agent = await users.GetByIdAsync(actorId, cancellationToken);

        return ticket.Assign(actorId, agent?.FullName ?? "Unknown", now);
    }
}
