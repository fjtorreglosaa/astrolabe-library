using Astrolabe.Application.Abstractions.Messaging;

namespace Astrolabe.Application.Features.Billing.Commands.AssessFine;

/// <summary>
/// Prices a late return. Implements BR-BIL-001 to BR-BIL-003, BR-BIL-009 and BR-BIL-010.
///
/// <para>
/// Reached from two directions on purpose: an event handler the moment the copy is checked in, and a
/// daily job that sweeps whatever the handler missed. Idempotent, so overlapping is harmless — the
/// unique index on the reservation is what makes that true and not merely intended.
/// </para>
/// </summary>
/// <returns>The fine's identifier, or null when nothing was owed.</returns>
public sealed record AssessFineCommand(Guid ReservationId) : ICommand<Guid?>;
