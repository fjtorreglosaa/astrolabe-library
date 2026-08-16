using Astrolabe.Application.Abstractions.Messaging;

namespace Astrolabe.Application.Features.Billing.Commands.RejectDeskPayment;

/// <summary>
/// The desk refusing a payment. Implements BR-BIL-019: the reason is mandatory, because rejecting
/// puts a debt back on somebody's account and they are entitled to know why.
/// </summary>
public sealed record RejectDeskPaymentCommand(string Code, string Reason) : ICommand;
