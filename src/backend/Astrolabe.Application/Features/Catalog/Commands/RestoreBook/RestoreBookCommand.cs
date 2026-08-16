using Astrolabe.Application.Abstractions.Messaging;

namespace Astrolabe.Application.Features.Catalog.Commands.RestoreBook;

/// <summary>Returns a removed book to the catalogue, keeping its reviews and rating. BR-CAT-021.</summary>
public sealed record RestoreBookCommand(Guid BookId) : ICommand;
