using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Abstractions.Network;
using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Features.Network.Errors;
using Astrolabe.Domain.Features.Network.Repositories;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Network.Commands.DeactivateLibrary;

public sealed class DeactivateLibraryCommandHandler(INetworkUnitOfWork network,
    ILibraryObligationsProbe obligations,
    ICurrentUser currentUser) : ICommandHandler<DeactivateLibraryCommand>
{
    public async Task<Result> Handle(
        DeactivateLibraryCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.Role is not UserRole.SuperAdmin)
        {
            return Result.Failure(NetworkErrors.SuperAdminRequired);
        }

        var library = await network.Libraries.GetByIdAsync(request.LibraryId, cancellationToken);

        if (library is null)
        {
            return Result.Failure(NetworkErrors.LibraryNotFound);
        }

        var city = await network.Cities.GetByIdAsync(library.CityId, cancellationToken);

        if (city is null)
        {
            return Result.Failure(NetworkErrors.CityNotFound);
        }

        // The handler gathers the facts; the entity judges them. That is what keeps BR-NET-005
        // testable without a database.
        var hasOpenObligations = await obligations.HasOpenObligationsAsync(
            library.Id, cancellationToken);

        var result = library.Deactivate(city.IsHomeLibrary(library.Id), hasOpenObligations);

        if (result.IsFailure)
        {
            return result;
        }

        await network.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
