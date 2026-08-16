using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Domain.Features.Billing.Errors;
using Astrolabe.Domain.Features.Billing.Repositories;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Billing.Commands.RemovePaymentMethod;

public sealed class RemovePaymentMethodCommandHandler(
    IBillingUnitOfWork billing,
    ICurrentUser currentUser) : ICommandHandler<RemovePaymentMethodCommand>
{
    public async Task<Result> Handle(
        RemovePaymentMethodCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } memberId)
        {
            return Result.Failure(BillingErrors.PaymentMethodNotFound);
        }

        // By member as well as by id: a card belonging to somebody else is not found rather than
        // refused after being read.
        var method = await billing.PaymentMethods.GetForMemberAsync(
            memberId, request.PaymentMethodId, cancellationToken);

        if (method is null)
        {
            return Result.Failure(BillingErrors.PaymentMethodNotFound);
        }

        var wasPrimary = method.IsPrimary;

        billing.PaymentMethods.Remove(method);

        // Removing the default must not leave the member without one, or the payment modal opens
        // with nothing selected and the pay button disabled for no visible reason.
        if (wasPrimary)
        {
            var remaining = (await billing.PaymentMethods.GetForMemberAsync(memberId, cancellationToken))
                .FirstOrDefault(m => m.Id != method.Id);

            remaining?.MakePrimary();
        }

        await billing.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
