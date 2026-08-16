using Astrolabe.Application.Abstractions.Messaging;

namespace Astrolabe.Application.Features.Catalog.Commands.SetBookCover;

/// <summary>
/// Uploads or replaces a book's cover. Backs BR-CAT-005.
///
/// <para>
/// Passing <c>null</c> content removes it, and the book falls back to its generated tint — which is
/// why removal is this command rather than another: "set the cover to nothing" and "set the cover"
/// share every check, and the fallback is not an error state to be handled separately.
/// </para>
/// </summary>
public sealed record SetBookCoverCommand(
    Guid BookId, string? ContentType, byte[]? Content) : ICommand<string?>;
