using Astrolabe.Domain.Features.Recommendations.Enums;

namespace Astrolabe.Presentation.Contracts.Recommendations;

/// <summary>
/// The body of "Save and test".
///
/// <para>
/// <see cref="Credential"/> is the only field in the whole API that carries a provider key, and it
/// travels in one direction. Nothing returns it — BR-REC-004 — and there is no endpoint that reads
/// one back, not even masked.
/// </para>
/// </summary>
public sealed record ConfigureLibraryAiRequest(AiProvider Provider, string Credential);
