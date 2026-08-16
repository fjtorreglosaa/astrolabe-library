using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Audit.Entities;
using Astrolabe.Domain.Features.Audit.Repositories;
using Astrolabe.Domain.Features.Catalog.Errors;
using Astrolabe.Domain.Features.Catalog.Repositories;
using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Features.Network.Errors;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Catalog.Commands.SendBookToRepair;

public sealed class SendBookToRepairCommandHandler(
    ICatalogUnitOfWork catalog,
    IAuditUnitOfWork audit,
    ICurrentUser currentUser,
    IDateTimeProvider clock) : ICommandHandler<SendBookToRepairCommand>
{
    public async Task<Result> Handle(
        SendBookToRepairCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.Role is not { } role || !role.IsStaff())
        {
            return Result.Failure(NetworkErrors.StaffRequired);
        }

        var book = await catalog.Books.GetByIdAsync(request.BookId, cancellationToken);

        if (book is null)
        {
            return Result.Failure(CatalogErrors.BookNotFound);
        }

        // The entity owns the transition rules. The handler only gathers what the decision needs
        // and persists the outcome.
        var result = book.SendToRepair(request.Reason, request.ExpectedBack, request.Notes, clock.UtcNow);

        if (result.IsFailure)
        {
            return result;
        }

        // BR-CAT-025: every lifecycle transition writes an audit entry recording who, what, when
        // and the stated reason. Staged here rather than in an event handler, because a reaction
        // runs after the commit and may be lost — and a trail that can silently miss a transition
        // is not a trail. Both units of work share one context, so this commits with the change.
        await audit.Entries.AddAsync(
            AuditEntry.Record(
                "catalog.book_sent_to_repair",
                clock.UtcNow,
                actorUserId: currentUser.UserId,
                detail: BuildDetail(request.Reason.ToString(), request.Notes)),
            cancellationToken);

        await catalog.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
    /// <summary>
    /// The typed reason and the free-text note, joined for the trail. The reason is what BR-CAT-023
    /// and BR-CAT-024 make mandatory; the note is whatever the librarian added.
    /// </summary>
    private static string BuildDetail(string reason, string? notes) =>
        string.IsNullOrWhiteSpace(notes) ? reason : $"{reason} — {notes.Trim()}";
}
