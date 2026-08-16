using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Domain.Features.Billing.Entities;
using Astrolabe.Domain.Features.Billing.Errors;
using Astrolabe.Domain.Features.Billing.Repositories;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Billing.Commands.AddPaymentMethod;

public sealed class AddPaymentMethodCommandHandler(
    IBillingUnitOfWork billing,
    ICurrentUser currentUser) : ICommandHandler<AddPaymentMethodCommand, Guid>
{
    public async Task<Result<Guid>> Handle(
        AddPaymentMethodCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } memberId)
        {
            return Result.Failure<Guid>(BillingErrors.PaymentMethodNotFound);
        }

        var existing = await billing.PaymentMethods.GetForMemberAsync(memberId, cancellationToken);

        // The first card on file is primary whatever the caller asked, so a member always has a
        // default and the payment modal never opens with nothing selected.
        var primary = request.MakePrimary || existing.Count == 0;

        var method = PaymentMethod.Create(
            memberId, request.Brand, request.Last4,
            request.ExpiryMonthYear, request.CardholderName, primary);

        if (method.IsFailure)
        {
            return Result.Failure<Guid>(method.Error);
        }

        if (primary)
        {
            // Exactly one primary. Demoting the others here rather than relying on a constraint
            // keeps the invariant true in memory as well as in the database.
            foreach (var other in existing)
            {
                other.MakeSecondary();
            }
        }

        await billing.PaymentMethods.AddAsync(method.Value, cancellationToken);
        await billing.SaveChangesAsync(cancellationToken);

        return Result.Success(method.Value.Id);
    }
}
