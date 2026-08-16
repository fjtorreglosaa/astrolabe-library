using Astrolabe.Domain.Features.Network.Repositories;

namespace Astrolabe.Infrastructure.Persistence.Repositories.Network;

/// <summary>
/// Composes the network repositories over one shared context, so their staged work commits together.
/// </summary>
public sealed class NetworkUnitOfWork(
    AstrolabeDbContext context,
    ICountryRepository countries,
    ICityRepository cities,
    ILibraryRepository libraries,
    ILibraryAssignmentRepository assignments,
    IAdminInvitationRepository invitations) : UnitOfWorkBase(context), INetworkUnitOfWork
{
    public ICountryRepository Countries { get; } = countries;

    public ICityRepository Cities { get; } = cities;

    public ILibraryRepository Libraries { get; } = libraries;

    public ILibraryAssignmentRepository Assignments { get; } = assignments;

    public IAdminInvitationRepository Invitations { get; } = invitations;
}
