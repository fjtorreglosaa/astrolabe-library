using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Domain.Features.Catalog.Enums;

namespace Astrolabe.Application.Features.Catalog.Commands.SendBookToRepair;

/// <summary>Withdraws a book for repair, with the reason BR-CAT-023 requires. Loans already running are unaffected.</summary>
public sealed record SendBookToRepairCommand(Guid BookId, RepairReason Reason, DateTimeOffset? ExpectedBack, string? Notes) : ICommand;
