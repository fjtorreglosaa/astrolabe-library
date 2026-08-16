using Astrolabe.Application.Abstractions.Messaging;

namespace Astrolabe.Application.Features.Billing.Commands.ValidateDeskPayment;

/// <summary>
/// A librarian confirming they took the money. Implements BR-BIL-005, BR-BIL-018 and BR-BIL-020.
/// The only thing that settles a desk payment.
/// </summary>
public sealed record ValidateDeskPaymentCommand(string Code) : ICommand;
