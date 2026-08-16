using Astrolabe.Application.Abstractions.Messaging;

namespace Astrolabe.Application.Features.Catalog.Commands.ReturnBookFromRepair;

/// <summary>Returns a repaired book to the shelf. BR-CAT-021.</summary>
public sealed record ReturnBookFromRepairCommand(Guid BookId) : ICommand;
