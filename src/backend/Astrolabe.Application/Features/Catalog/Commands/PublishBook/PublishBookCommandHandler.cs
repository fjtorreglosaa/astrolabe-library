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

namespace Astrolabe.Application.Features.Catalog.Commands.PublishBook;

public sealed class PublishBookCommandHandler(
    ICatalogUnitOfWork catalog,
    IAuditUnitOfWork audit,
    ICurrentUser currentUser,
    IDateTimeProvider clock) : ICommandHandler<PublishBookCommand>
{
    public async Task<Result> Handle(
        PublishBookCommand request, CancellationToken cancellationToken)
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
        var result = book.Publish(clock.UtcNow);

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
                "catalog.book_published",
                clock.UtcNow,
                actorUserId: currentUser.UserId,
                detail: null),
            cancellationToken);

        await catalog.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }}
