using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Contracts.Catalog;

namespace Astrolabe.Application.Features.Catalog.Queries.GetBookCover;

/// <summary>
/// The bytes of one cover. Its own query because it is its own HTTP resource — that is the whole
/// point of not putting the image in the listing.
/// </summary>
public sealed record GetBookCoverQuery(Guid BookId) : IQuery<BookCoverDto>;
