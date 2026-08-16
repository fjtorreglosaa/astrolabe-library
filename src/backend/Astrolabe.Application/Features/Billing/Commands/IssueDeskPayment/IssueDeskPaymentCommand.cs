using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Contracts.Billing;

namespace Astrolabe.Application.Features.Billing.Commands.IssueDeskPayment;

/// <summary>
/// Produces a code the member takes to a library counter. Implements BR-BIL-004, BR-BIL-017 and
/// BR-BIL-021.
///
/// It settles nothing. Nobody has paid when a code is printed.
/// </summary>
public sealed record IssueDeskPaymentCommand(IReadOnlyList<Guid> FineIds) : ICommand<DeskPaymentDto>;
