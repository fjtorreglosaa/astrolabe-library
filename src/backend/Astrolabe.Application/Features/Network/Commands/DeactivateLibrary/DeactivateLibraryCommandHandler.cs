using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Abstractions.Network;
using Astrolabe.Application.Contracts.Network;
using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Features.Network.Errors;
using Astrolabe.Domain.Features.Network.Repositories;
using Astrolabe.Domain.Primitives;
using Microsoft.Extensions.Logging;

namespace Astrolabe.Application.Features.Network.Commands.DeactivateLibrary;

public sealed class DeactivateLibraryCommandHandler(INetworkUnitOfWork network,
    ILibraryObligationsProbe obligations,
    ICurrentUser currentUser,
    ILogger<DeactivateLibraryCommandHandler> logger)
    : ICommandHandler<DeactivateLibraryCommand, LibraryObligations>
{
    public async Task<Result<LibraryObligations>> Handle(
        DeactivateLibraryCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.Role is not UserRole.SuperAdmin)
        {
            return Result.Failure<LibraryObligations>(NetworkErrors.SuperAdminRequired);
        }

        var library = await network.Libraries.GetByIdAsync(request.LibraryId, cancellationToken);

        if (library is null)
        {
            return Result.Failure<LibraryObligations>(NetworkErrors.LibraryNotFound);
        }

        var city = await network.Cities.GetByIdAsync(library.CityId, cancellationToken);

        if (city is null)
        {
            return Result.Failure<LibraryObligations>(NetworkErrors.CityNotFound);
        }

        // The handler gathers the facts; the entity judges them. That is what keeps BR-NET-005
        // testable without a database.
        var result = library.Deactivate(city.IsHomeLibrary(library.Id));

        if (result.IsFailure)
        {
            return Result.Failure<LibraryObligations>(result.Error);
        }

        // Read after the decision, not before it: this is a report for the operator, not a
        // precondition, and gathering it first would suggest otherwise to the next reader.
        var outstanding = await obligations.GetAsync(library.Id, cancellationToken);

        await network.SaveChangesAsync(cancellationToken);

        if (outstanding.HasAny)
        {
            // Worth a log line of its own: a branch withdrawn with work still on it needs a human
            // to wind it down, and nothing else in the system will chase it.
            logger.LogWarning(
                "Library {LibraryId} was deactivated while still holding {Copies} copies, "
                + "{Reservations} active reservation(s) and {Fines} unresolved fine(s). "
                + "Returns and fine payments remain available to staff.",
                library.Id, outstanding.Copies, outstanding.ActiveReservations,
                outstanding.UnresolvedFines);
        }

        return Result.Success(outstanding);
    }
}
