using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Abstractions.Network;
using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Catalog.Entities;
using Astrolabe.Domain.Features.Catalog.Errors;
using Astrolabe.Domain.Features.Catalog.Repositories;
using Astrolabe.Domain.Features.Catalog.ValueObjects;
using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Features.Network.Errors;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Catalog.Commands.CreateBookDraft;

public sealed class CreateBookDraftCommandHandler(
    ICatalogUnitOfWork catalog,
    ILibraryScopeProvider scope,
    ICurrentUser currentUser,
    IDateTimeProvider clock) : ICommandHandler<CreateBookDraftCommand, Guid>
{
    public async Task<Result<Guid>> Handle(
        CreateBookDraftCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.Role is not { } role || !role.IsStaff())
        {
            return Result.Failure<Guid>(NetworkErrors.StaffRequired);
        }

        var isbn = Isbn.Create(request.Isbn);

        if (isbn.IsFailure)
        {
            return Result.Failure<Guid>(isbn.Error);
        }

        // The unique index is the real guard against a race; this exists so the ordinary case gets
        // a message that names the problem instead of a constraint violation.
        if (await catalog.Books.ExistsWithIsbnAsync(isbn.Value.Value, cancellationToken))
        {
            return Result.Failure<Guid>(CatalogErrors.IsbnAlreadyExists);
        }

        var book = Book.CreateDraft(
            isbn.Value, request.Title, request.Author, request.Publisher, request.Genre,
            request.Tier, Money.FromCents(request.RetailPriceCents), request.CoverUrl, clock.UtcNow);

        if (book.IsFailure)
        {
            return Result.Failure<Guid>(book.Error);
        }

        // BR-NET-006: an administrator may only place stock in libraries assigned to them. Checked
        // per allocation, so one library outside their scope fails the whole request rather than
        // silently dropping that shelf.
        var reach = await scope.GetCurrentScopeAsync(cancellationToken);

        foreach (var allocation in request.Copies)
        {
            if (!reach.Covers(allocation.LibraryId))
            {
                return Result.Failure<Guid>(NetworkErrors.LibraryOutOfScope);
            }

            var added = book.Value.AddCopies(allocation.LibraryId, allocation.Quantity);

            if (added.IsFailure)
            {
                return Result.Failure<Guid>(added.Error);
            }
        }

        await catalog.Books.AddAsync(book.Value, cancellationToken);
        await catalog.SaveChangesAsync(cancellationToken);

        return Result.Success(book.Value.Id);
    }
}
