using Astrolabe.Domain.Abstractions.Persistence;
using Astrolabe.Domain.Features.Billing.Entities;

namespace Astrolabe.Domain.Features.Billing.Repositories;

/// <summary>Persistence for <see cref="PaymentMethod"/>.</summary>
public interface IPaymentMethodRepository : IRepository<PaymentMethod>
{
    Task<IReadOnlyList<PaymentMethod>> GetForMemberAsync(
        Guid memberId, CancellationToken cancellationToken = default);

    /// <summary>Looked up by member as well as by id, so one member cannot pay with another's card.</summary>
    Task<PaymentMethod?> GetForMemberAsync(
        Guid memberId, Guid paymentMethodId, CancellationToken cancellationToken = default);
}
