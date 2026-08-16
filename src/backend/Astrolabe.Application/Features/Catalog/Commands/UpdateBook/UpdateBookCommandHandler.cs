using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Domain.Features.Catalog.Errors;
using Astrolabe.Domain.Features.Catalog.Repositories;
using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Features.Network.Errors;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Catalog.Commands.UpdateBook;

public sealed class UpdateBookCommandHandler(
    ICatalogUnitOfWork catalog,
    ICurrentUser currentUser) : ICommandHandler<UpdateBookCommand>
{
    public async Task<Result> Handle(UpdateBookCommand request, CancellationToken cancellationToken)
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

        // The ISBN is deliberately not editable. It identifies the work, and changing it would turn
        // one book into another while keeping its reviews and its loan history.
        var result = book.UpdateDetails(
            request.Title, request.Author, request.Publisher, request.Genre,
            request.Tier, Money.FromCents(request.RetailPriceCents), request.CoverUrl);

        if (result.IsFailure)
        {
            return result;
        }

        await catalog.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
