using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Contracts.Catalog;

namespace Astrolabe.Application.Features.Catalog.Queries.GetBookDetail;

/// <summary>One book with every branch that holds it. Implements BR-CAT-010 and BR-CAT-016.</summary>
public sealed record GetBookDetailQuery(Guid BookId) : IQuery<BookDetailDto>;
