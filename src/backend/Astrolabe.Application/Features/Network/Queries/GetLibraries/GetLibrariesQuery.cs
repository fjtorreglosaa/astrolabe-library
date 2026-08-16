using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Contracts.Network;

namespace Astrolabe.Application.Features.Network.Queries.GetLibraries;

/// <summary>Libraries, optionally narrowed to one city.</summary>
public sealed record GetLibrariesQuery(Guid? CityId = null) : IQuery<IReadOnlyList<LibraryDto>>;
