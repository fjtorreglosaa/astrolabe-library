using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Contracts.Billing;

namespace Astrolabe.Application.Features.Billing.Commands.PayFines;

/// <summary>
/// Settles one or more of the caller's fines by card. Implements BR-BIL-008 and BR-BIL-014 to
/// BR-BIL-016.
///
/// <para>
/// No member identifier: BR-BIL-016 is enforced by the contract rather than by a check inside it.
/// </para>
/// <para>
/// No idempotency key either, unlike <c>ConfirmReservationCommand</c> — and the difference is worth
/// stating. A reservation takes a copy off a shelf, which is a new fact each time and needs a key to
/// deduplicate. A payment settles named fines, and settling an already-settled fine is naturally a
/// no-op. The fine's own state is the idempotency, so a key would be a second mechanism guarding
/// something already guarded.
/// </para>
/// </summary>
public sealed record PayFinesCommand(
    IReadOnlyList<Guid> FineIds,
    Guid PaymentMethodId) : ICommand<PaymentReceiptDto>;
