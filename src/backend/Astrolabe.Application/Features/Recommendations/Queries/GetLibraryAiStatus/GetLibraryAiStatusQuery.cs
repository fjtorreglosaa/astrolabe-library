using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Contracts.Recommendations;

namespace Astrolabe.Application.Features.Recommendations.Queries.GetLibraryAiStatus;

/// <summary>
/// The configuration panel: every library the caller administers, and whether it is connected.
///
/// Takes no library identifier. The answer is "yours", and letting a caller name one would make the
/// scope check something a handler has to remember rather than something the shape guarantees.
/// </summary>
public sealed record GetLibraryAiStatusQuery : IQuery<IReadOnlyList<LibraryAiStatusDto>>;
