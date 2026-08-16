using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Features.Network.Errors;
using Astrolabe.Domain.Features.Network.Repositories;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Network.Commands.DesignateHomeLibrary;

public sealed class DesignateHomeLibraryCommandHandler(INetworkUnitOfWork network,
    ICurrentUser currentUser) : ICommandHandler<DesignateHomeLibraryCommand>
{
    public async Task<Result> Handle(
        DesignateHomeLibraryCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.Role is not UserRole.SuperAdmin)
        {
            return Result.Failure(NetworkErrors.SuperAdminRequired);
        }

        var city = await network.Cities.GetByIdAsync(request.CityId, cancellationToken);

        if (city is null)
        {
            return Result.Failure(NetworkErrors.CityNotFound);
        }

        var library = await network.Libraries.GetByIdAsync(request.LibraryId, cancellationToken);

        if (library is null)
        {
            return Result.Failure(NetworkErrors.LibraryNotFound);
        }

        // The entity verifies that the library belongs to this city and is active. Passing the
        // library rather than its identifier is what makes those checks possible without a lookup.
        var result = city.DesignateHomeLibrary(library);

        if (result.IsFailure)
        {
            return result;
        }

        await network.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
