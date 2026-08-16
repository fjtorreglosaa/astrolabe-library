using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Contracts.Recommendations;
using Astrolabe.Domain.Features.Recommendations.Enums;

namespace Astrolabe.Application.Features.Recommendations.Commands.ConfigureLibraryAi;

/// <summary>
/// The prototype's "Save and test". Implements BR-REC-001, BR-REC-008 and BR-REC-013.
/// </summary>
/// <param name="Credential">
/// Plaintext, and <b>inbound only</b>. It is encrypted before it is stored and never travels back
/// out — BR-REC-004. This is the one place in the system where a provider key exists in the clear,
/// and it exists there for the length of one request.
/// </param>
public sealed record ConfigureLibraryAiCommand(
    Guid LibraryId,
    AiProvider Provider,
    string Credential) : ICommand<LibraryAiStatusDto>;
