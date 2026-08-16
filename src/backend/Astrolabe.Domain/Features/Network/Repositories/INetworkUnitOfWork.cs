using Astrolabe.Domain.Abstractions.Persistence;

namespace Astrolabe.Domain.Features.Network.Repositories;

/// <summary>
/// The network bounded context's unit of work. Exposes only this context's repositories.
/// See <c>IIdentityUnitOfWork</c> for the rationale.
/// </summary>
public interface INetworkUnitOfWork : IUnitOfWork
{
    ICountryRepository Countries { get; }

    ICityRepository Cities { get; }

    ILibraryRepository Libraries { get; }

    ILibraryAssignmentRepository Assignments { get; }

    IAdminInvitationRepository Invitations { get; }
}
