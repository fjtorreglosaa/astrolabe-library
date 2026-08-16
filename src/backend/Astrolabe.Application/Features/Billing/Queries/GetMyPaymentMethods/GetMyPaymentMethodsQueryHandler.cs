using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Contracts.Billing;
using Astrolabe.Application.Shared.Billing;
using Astrolabe.Domain.Features.Billing.Errors;
using Astrolabe.Domain.Features.Billing.Repositories;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Billing.Queries.GetMyPaymentMethods;

public sealed class GetMyPaymentMethodsQueryHandler(
    IBillingUnitOfWork billing,
    ICurrentUser currentUser) : IQueryHandler<GetMyPaymentMethodsQuery, IReadOnlyList<PaymentMethodDto>>
{
    public async Task<Result<IReadOnlyList<PaymentMethodDto>>> Handle(
        GetMyPaymentMethodsQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } memberId)
        {
            return Result.Failure<IReadOnlyList<PaymentMethodDto>>(BillingErrors.PaymentMethodNotFound);
        }

        var methods = await billing.PaymentMethods.GetForMemberAsync(memberId, cancellationToken);

        return Result.Success<IReadOnlyList<PaymentMethodDto>>(
            methods.Select(BillingProjection.ToDto).ToList());
    }
}
