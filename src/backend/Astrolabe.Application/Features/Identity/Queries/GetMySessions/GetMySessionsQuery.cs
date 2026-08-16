using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Contracts.Identity;

namespace Astrolabe.Application.Features.Identity.Queries.GetMySessions;

/// <summary>
/// The caller's live sessions. Implements BR-IDN-021, BR-IDN-025 and BR-IDN-026.
/// </summary>
public sealed record GetMySessionsQuery : IQuery<IReadOnlyList<SessionDto>>;
