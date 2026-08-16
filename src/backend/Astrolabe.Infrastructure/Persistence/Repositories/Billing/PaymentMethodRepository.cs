using Astrolabe.Domain.Features.Billing.Entities;
using Astrolabe.Domain.Features.Billing.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Astrolabe.Infrastructure.Persistence.Repositories.Billing;

public sealed class PaymentMethodRepository(AstrolabeDbContext context)
    : Repository<PaymentMethod>(context), IPaymentMethodRepository
{
    public async Task<IReadOnlyList<PaymentMethod>> GetForMemberAsync(
        Guid memberId, CancellationToken cancellationToken = default) =>
        await Query
            .Where(p => p.MemberId == memberId)
            .OrderByDescending(p => p.IsPrimary)
            .ThenBy(p => p.Id)
            .ToListAsync(cancellationToken);

    public async Task<PaymentMethod?> GetForMemberAsync(
        Guid memberId, Guid paymentMethodId, CancellationToken cancellationToken = default) =>
        // Both keys, always. A card is looked up by member as well as by identifier so one member
        // can never pay with another's.
        await Query.FirstOrDefaultAsync(
            p => p.Id == paymentMethodId && p.MemberId == memberId, cancellationToken);
}
