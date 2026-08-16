using Astrolabe.Application.Abstractions.Messaging;

namespace Astrolabe.Application.Features.Billing.Commands.RemovePaymentMethod;

/// <summary>Takes a card off file. Never removes anything from the ledger it paid for.</summary>
public sealed record RemovePaymentMethodCommand(Guid PaymentMethodId) : ICommand;
