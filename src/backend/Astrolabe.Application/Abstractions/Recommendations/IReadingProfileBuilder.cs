using Astrolabe.Application.Contracts.Recommendations;

namespace Astrolabe.Application.Abstractions.Recommendations;

/// <summary>
/// Builds the anonymised payload a provider is given. BR-REC-005 lives here and nowhere else.
///
/// <para>
/// One builder, because a rule about a payload is only enforceable if exactly one place builds it.
/// Two would be two chances to include an email, and the second one would be written by somebody who
/// had not read this rule.
/// </para>
/// </summary>
public interface IReadingProfileBuilder
{
    Task<ReadingProfile> BuildAsync(Guid memberId, CancellationToken cancellationToken = default);
}
