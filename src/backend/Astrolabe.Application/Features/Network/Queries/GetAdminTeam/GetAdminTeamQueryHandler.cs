using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Contracts.Network;
using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Features.Identity.Repositories;
using Astrolabe.Domain.Features.Network.Errors;
using Astrolabe.Domain.Features.Network.Repositories;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Network.Queries.GetAdminTeam;

public sealed class GetAdminTeamQueryHandler(
    IIdentityUnitOfWork identity,
    INetworkUnitOfWork network,
    ICurrentUser currentUser) : IQueryHandler<GetAdminTeamQuery, IReadOnlyList<AdminDto>>
{
    public async Task<Result<IReadOnlyList<AdminDto>>> Handle(
        GetAdminTeamQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.Role is not UserRole.SuperAdmin)
        {
            return Result.Failure<IReadOnlyList<AdminDto>>(NetworkErrors.SuperAdminRequired);
        }

        var staff = await identity.Users.ListByFilterAsync(
            u => (u.Role == UserRole.Admin || u.Role == UserRole.SuperAdmin)
                 && u.Status != UserStatus.Deleted,
            cancellationToken);

        var libraries = await network.Libraries.GetAllAsync(cancellationToken);
        var libraryNames = libraries.ToDictionary(l => l.Id, l => l.Name);

        var admins = new List<AdminDto>(staff.Count);

        foreach (var user in staff)
        {
            var assignments = await network.Assignments.GetActiveLibraryIdsByUserAsync(
                user.Id, cancellationToken);

            // A super administrator reaches every library without an assignment (BR-NET-007), so
            // showing their empty assignment list would misrepresent what they can do.
            IReadOnlyList<string> names = user.Role is UserRole.SuperAdmin
                ? [.. libraryNames.Values.Order()]
                : [.. assignments.Select(id => libraryNames.GetValueOrDefault(id, "—")).Order()];

            admins.Add(new AdminDto(
                user.Id, user.Email.Value, user.FullName, user.Role, user.Status, names, user.CreatedAt));
        }

        IReadOnlyList<AdminDto> result = [.. admins.OrderBy(a => a.FullName)];

        return Result.Success(result);
    }
}
