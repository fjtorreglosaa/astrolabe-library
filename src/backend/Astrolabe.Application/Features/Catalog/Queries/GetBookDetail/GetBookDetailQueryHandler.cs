using Astrolabe.Application.Abstractions.Membership;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Abstractions.Network;
using Astrolabe.Application.Contracts.Catalog;
using Astrolabe.Application.Shared.Catalog;
using Astrolabe.Domain.Features.Catalog.Errors;
using Astrolabe.Domain.Features.Catalog.Repositories;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Catalog.Queries.GetBookDetail;

public sealed class GetBookDetailQueryHandler(
    ICatalogUnitOfWork catalog,
    IEntitlementProvider entitlements,
    ILibraryLocationProvider libraries)
    : IQueryHandler<GetBookDetailQuery, BookDetailDto>
{
    public async Task<Result<BookDetailDto>> Handle(
        GetBookDetailQuery request, CancellationToken cancellationToken)
    {
        var book = await catalog.Books.GetWithCopiesAsync(request.BookId, cancellationToken);

        // BR-CAT-020: a draft, a book in repair and a removed book are not findable by a member.
        // Answering "not found" rather than "not visible" avoids confirming that the book exists.
        if (book is null || !book.IsVisibleToMembers)
        {
            return Result.Failure<BookDetailDto>(CatalogErrors.BookNotFound);
        }

        var member = await entitlements.GetForCurrentMemberAsync(cancellationToken);
        var locations = await libraries.GetAllAsync(cancellationToken);

        // BR-CAT-016: returned whatever the verdict. A book the member cannot borrow still opens.
        return Result.Success(BookProjection.ToDetail(book, member, locations));
    }
}
