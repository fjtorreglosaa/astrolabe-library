using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Abstractions.Network;
using Astrolabe.Application.Contracts.Billing;
using Astrolabe.Application.Shared.Billing;
using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Billing.Repositories;
using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Features.Identity.Repositories;
using Astrolabe.Domain.Features.Network.Errors;
using Astrolabe.Domain.Features.Network.Repositories;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Billing.Queries.GetDeskPayments;

public sealed class GetDeskPaymentsQueryHandler(
    IBillingUnitOfWork billing,
    INetworkUnitOfWork network,
    IIdentityUnitOfWork identity,
    ILibraryLocationProvider libraries,
    ILibraryScopeProvider scope,
    ICurrentUser currentUser,
    IDateTimeProvider clock) : IQueryHandler<GetDeskPaymentsQuery, PagedResult<DeskPaymentDto>>
{
    public async Task<Result<PagedResult<DeskPaymentDto>>> Handle(
        GetDeskPaymentsQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.Role is not { } role || !role.IsStaff())
        {
            return Result.Failure<PagedResult<DeskPaymentDto>>(NetworkErrors.StaffRequired);
        }

        var reach = await scope.GetCurrentScopeAsync(cancellationToken);

        // A super administrator has no assignment list, so the whole network stands in for one. An
        // administrator with no assignments gets an empty set, which the repository turns into an
        // empty page rather than into everything.
        var libraryIds = reach.IsUnrestricted
            ? (await network.Libraries.GetAllAsync(cancellationToken)).Select(l => l.Id).ToList()
            : reach.LibraryIds.ToList();

        var page = await billing.DeskPayments.GetForLibrariesAsync(
            libraryIds, request.Status, request.Page, request.PageSize, cancellationToken);

        // The members and the fines are each fetched once for the page rather than per row.
        var memberIds = page.Items.Select(p => p.MemberId).Distinct().ToList();
        var members = (await identity.Users.GetByIdsAsync(memberIds, cancellationToken))
            .ToDictionary(u => u.Id, u => u.FullName);

        var locations = await libraries.GetAllAsync(cancellationToken);
        var now = clock.UtcNow;

        var items = new List<DeskPaymentDto>(page.Items.Count);

        foreach (var payment in page.Items)
        {
            var fines = await billing.Fines.GetByDeskPaymentAsync(payment.Id, cancellationToken);

            items.Add(BillingProjection.ToDto(
                payment,
                fines,
                // A deleted account's code stays on the queue: the library is still owed the money.
                members.GetValueOrDefault(payment.MemberId) ?? "Former member",
                locations,
                now));
        }

        return Result.Success(PagedResult<DeskPaymentDto>.Create(
            items, page.Page, page.PageSize, page.TotalCount));
    }
}
