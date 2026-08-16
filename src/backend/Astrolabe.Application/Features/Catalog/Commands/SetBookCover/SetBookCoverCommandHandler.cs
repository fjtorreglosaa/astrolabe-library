using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Audit.Entities;
using Astrolabe.Domain.Features.Audit.Repositories;
using Astrolabe.Domain.Features.Catalog.Entities;
using Astrolabe.Domain.Features.Catalog.Errors;
using Astrolabe.Domain.Features.Catalog.Repositories;
using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Features.Network.Errors;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Catalog.Commands.SetBookCover;

public sealed class SetBookCoverCommandHandler(
    ICatalogUnitOfWork catalog,
    IAuditUnitOfWork audit,
    ICurrentUser currentUser,
    IDateTimeProvider clock) : ICommandHandler<SetBookCoverCommand, string?>
{
    public async Task<Result<string?>> Handle(
        SetBookCoverCommand request, CancellationToken cancellationToken)
    {
        if (currentUser is not { UserId: { } actorId, Role: { } role })
        {
            return Result.Failure<string?>(NetworkErrors.StaffRequired);
        }

        if (!role.IsStaff())
        {
            return Result.Failure<string?>(NetworkErrors.StaffRequired);
        }

        var book = await catalog.Books.GetByIdAsync(request.BookId, cancellationToken);

        if (book is null)
        {
            return Result.Failure<string?>(CatalogErrors.BookNotFound);
        }

        var existing = await catalog.Books.GetCoverAsync(book.Id, cancellationToken);
        var now = clock.UtcNow;

        // No content means remove. The book keeps its identity and falls back to the tint that
        // BR-CAT-005 derives from it, which is a normal state rather than a missing one.
        if (request.Content is null || request.Content.Length == 0)
        {
            if (existing is not null)
            {
                catalog.Books.RemoveCover(existing);
            }

            book.SetCoverUrl(null);
            await Commit(actorId, book.Id, "removed", now, cancellationToken);

            return Result.Success<string?>(null);
        }

        if (existing is null)
        {
            var created = BookCoverImage.Create(
                book.Id, request.ContentType, request.Content, now);

            if (created.IsFailure)
            {
                return Result.Failure<string?>(created.Error);
            }

            await catalog.Books.AddCoverAsync(created.Value, cancellationToken);
        }
        else
        {
            // Replaced in place, so a book keeps one cover row for its whole life and the URL that
            // points at it never has to change.
            var replaced = existing.Replace(request.ContentType, request.Content, now);

            if (replaced.IsFailure)
            {
                return Result.Failure<string?>(replaced.Error);
            }
        }

        // A path, not the bytes. Everything downstream — the DTOs, the BookCover component — already
        // takes a URL, so an uploaded cover and an external one are the same thing to all of them.
        var url = $"/api/v1/catalog/books/{book.Id}/cover";

        book.SetCoverUrl(url);
        await Commit(actorId, book.Id, "set", now, cancellationToken);

        return Result.Success<string?>(url);
    }

    private async Task Commit(
        Guid actorId, Guid bookId, string action, DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await audit.Entries.AddAsync(
            AuditEntry.Record(
                $"catalog.cover_{action}", now, actorUserId: actorId, subjectUserId: null,
                detail: bookId.ToString()),
            cancellationToken);

        await catalog.SaveChangesAsync(cancellationToken);
    }
}
