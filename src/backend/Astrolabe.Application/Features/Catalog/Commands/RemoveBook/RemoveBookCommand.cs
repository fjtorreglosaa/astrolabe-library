using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Domain.Features.Catalog.Enums;

namespace Astrolabe.Application.Features.Catalog.Commands.RemoveBook;

/// <summary>Removes a book from the collection, with the reason BR-CAT-024 requires. Restorable.</summary>
public sealed record RemoveBookCommand(Guid BookId, RemovalReason Reason, string? Notes) : ICommand;
