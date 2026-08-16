using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Features.Network.Entities;
using Astrolabe.Domain.Features.Network.Errors;
using Astrolabe.Domain.Features.Network.Repositories;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Network.Commands.CreateLibrary;

public sealed class CreateLibraryCommandHandler(INetworkUnitOfWork network,
    ICurrentUser currentUser) : ICommandHandler<CreateLibraryCommand, Guid>
{
    public async Task<Result<Guid>> Handle(
        CreateLibraryCommand request, CancellationToken cancellationToken)
    {
        // Validation runs inside the handler — there are no pipeline behaviors in this solution.
        if (currentUser.Role is not UserRole.SuperAdmin)
        {
            return Result.Failure<Guid>(NetworkErrors.SuperAdminRequired);
        }

        var city = await network.Cities.GetByIdAsync(request.CityId, cancellationToken);

        if (city is null)
        {
            return Result.Failure<Guid>(NetworkErrors.CityNotFound);
        }

        var name = request.Name?.Trim() ?? string.Empty;

        // Checked before inserting so the caller gets a clean conflict rather than a database
        // constraint violation. The unique index remains the real guard against a race.
        if (await network.Libraries.ExistsWithNameInCityAsync(city.Id, name, cancellationToken))
        {
            return Result.Failure<Guid>(NetworkErrors.LibraryNameTakenInCity);
        }

        var library = Library.Create(Guid.NewGuid(), city.Id, name);

        if (library.IsFailure)
        {
            return Result.Failure<Guid>(library.Error);
        }

        await network.Libraries.AddAsync(library.Value, cancellationToken);

        // The first library of a city becomes its home library, so BR-NET-003 can never be left
        // unsatisfied by adding a branch to a city that had none.
        if (city.HomeLibraryId is null)
        {
            var designated = city.DesignateHomeLibrary(library.Value);

            if (designated.IsFailure)
            {
                return Result.Failure<Guid>(designated.Error);
            }
        }

        await network.SaveChangesAsync(cancellationToken);

        return Result.Success(library.Value.Id);
    }
}
