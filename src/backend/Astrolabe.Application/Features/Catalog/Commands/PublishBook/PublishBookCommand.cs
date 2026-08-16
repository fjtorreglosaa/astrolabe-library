using Astrolabe.Application.Abstractions.Messaging;

namespace Astrolabe.Application.Features.Catalog.Commands.PublishBook;

/// <summary>Moves a draft into the catalogue, where members can find it. BR-CAT-021.</summary>
public sealed record PublishBookCommand(Guid BookId) : ICommand;
