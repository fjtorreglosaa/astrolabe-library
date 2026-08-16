using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Abstractions.Network;
using Astrolabe.Application.Contracts.Network;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Network.Queries.GetMyScope;

public sealed class GetMyScopeQueryHandler(ILibraryScopeProvider scopeProvider)
    : IQueryHandler<GetMyScopeQuery, LibraryScopeDto>
{
    public async Task<Result<LibraryScopeDto>> Handle(
        GetMyScopeQuery request, CancellationToken cancellationToken)
    {
        var scope = await scopeProvider.GetCurrentScopeAsync(cancellationToken);

        // An empty scope is a valid answer, not an error: BR-NET-010 says an administrator with no
        // assignments sees empty lists.
        return Result.Success(new LibraryScopeDto(scope.IsUnrestricted, [.. scope.LibraryIds]));
    }
}
