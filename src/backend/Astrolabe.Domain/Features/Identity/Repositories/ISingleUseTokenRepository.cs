using Astrolabe.Domain.Abstractions.Persistence;
using Astrolabe.Domain.Features.Identity.Entities;
using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Features.Identity.ValueObjects;

namespace Astrolabe.Domain.Features.Identity.Repositories;

/// <summary>Persistence for <see cref="SingleUseToken"/>.</summary>
public interface ISingleUseTokenRepository : IRepository<SingleUseToken>
{
    /// <summary>Looked up by hash: the plaintext is never stored, so it cannot be searched for.</summary>
    Task<SingleUseToken?> GetUsableByHashAsync(
        SecretHash hash, SingleUseTokenPurpose purpose, CancellationToken cancellationToken = default);

    /// <summary>
    /// The user's outstanding tokens of a purpose. Fetched so issuing a new one can retire them,
    /// which is what BR-IDN-005 requires.
    /// </summary>
    Task<IReadOnlyList<SingleUseToken>> GetOutstandingAsync(
        Guid userId, SingleUseTokenPurpose purpose, CancellationToken cancellationToken = default);
}
