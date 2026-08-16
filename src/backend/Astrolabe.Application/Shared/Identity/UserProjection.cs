using Astrolabe.Application.Contracts.Identity;
using Astrolabe.Application.Shared.Catalog;
using Astrolabe.Domain.Features.Identity.Entities;
using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Features.Identity.Policies;
using Astrolabe.Domain.Features.Membership.Enums;

namespace Astrolabe.Application.Shared.Identity;

/// <summary>
/// Turns a user into the shapes the staff directory renders.
///
/// <para>
/// Sits in <c>Shared</c> for the same reason <c>BookProjection</c> does: the list and the detail
/// panel both need the geography and the "may I act on this row" verdict, and computing the second
/// in two places would give two chances to disagree with the handler that enforces it.
/// </para>
/// </summary>
public static class UserProjection
{
    public static UserSummaryDto ToSummary(
        User user,
        Guid actorId,
        UserRole actorRole,
        IReadOnlyDictionary<Guid, PlanTier> plans,
        IReadOnlyDictionary<Guid, BookProjection.LibraryLocation> libraries,
        IReadOnlyDictionary<Guid, Guid>? homeLibraryByCity = null)
    {
        var (cityName, homeLibraryName) = GeographyOf(user, libraries, homeLibraryByCity);

        return new UserSummaryDto(
            user.Id,
            user.FullName,
            user.Email.Value,
            user.Role,
            // Absent rather than defaulted. Staff hold no subscription, and so does a member whose
            // account was created before membership existed — reporting Basic for either would put
            // a tier on somebody who never bought one.
            plans.TryGetValue(user.Id, out var plan) ? plan : null,
            user.Status,
            cityName,
            homeLibraryName,
            user.CreatedAt,
            UserAdministrationPolicy.CanAdminister(actorId, actorRole, user.Id, user.Role));
    }

    public static UserDetailDto ToDetail(
        User user,
        Guid actorId,
        UserRole actorRole,
        PlanTier? plan,
        IReadOnlyDictionary<Guid, BookProjection.LibraryLocation> libraries,
        IReadOnlyDictionary<Guid, Guid> homeLibraryByCity,
        DateTimeOffset? lastActiveAt,
        int activeReservations,
        int outstandingFineCents,
        int purchases,
        int? onTimeReturnPercent)
    {
        var (cityName, homeLibraryName) = GeographyOf(user, libraries, homeLibraryByCity);
        var verdict = UserAdministrationPolicy.EnsureCanAdminister(
            actorId, actorRole, user.Id, user.Role);

        return new UserDetailDto(
            user.Id,
            user.FullName,
            user.Email.Value,
            user.Role,
            plan,
            user.Status,
            cityName,
            homeLibraryName,
            user.CreatedAt,
            lastActiveAt,
            activeReservations,
            outstandingFineCents,
            purchases,
            onTimeReturnPercent,
            verdict.IsSuccess,
            // The reason travels with the verdict. The prototype is explicit that a control which
            // cannot be used has to say why, and a greyed-out button with no explanation is what
            // makes an administrator think the console is broken.
            verdict.IsSuccess ? null : verdict.Error.Message);
    }

    /// <summary>
    /// A member's city and the branch they borrow from. Both null for staff, who have neither.
    /// </summary>
    private static (string? CityName, string? HomeLibraryName) GeographyOf(
        User user,
        IReadOnlyDictionary<Guid, BookProjection.LibraryLocation> libraries,
        IReadOnlyDictionary<Guid, Guid>? homeLibraryByCity)
    {
        if (user.CityId is not { } cityId)
        {
            return (null, null);
        }

        // Any library in the city carries its name, so the city is resolved without a second lookup.
        var cityName = libraries.Values
            .FirstOrDefault(location => location.CityId == cityId)?.CityName;

        if (homeLibraryByCity is null
            || !homeLibraryByCity.TryGetValue(cityId, out var homeLibraryId))
        {
            return (cityName, null);
        }

        return (cityName, libraries.GetValueOrDefault(homeLibraryId)?.LibraryName);
    }
}
