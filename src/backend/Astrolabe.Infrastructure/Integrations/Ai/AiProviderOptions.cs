using System.ComponentModel.DataAnnotations;

namespace Astrolabe.Infrastructure.Integrations.Ai;

/// <summary>
/// Where the vendors live and how long they get.
///
/// <para>
/// <b>No credential here.</b> Keys belong to libraries, not to the deployment — BR-REC-001 — so
/// there is nothing in configuration for one to be pasted into.
/// </para>
/// </summary>
public sealed class AiProviderOptions
{
    public const string SectionName = "Ai";

    [Required, Url]
    public string ClaudeBaseUrl { get; init; } = "https://api.anthropic.com";

    [Required, Url]
    public string OpenAiBaseUrl { get; init; } = "https://api.openai.com";

    public string ClaudeModel { get; init; } = "claude-sonnet-4-5";

    public string OpenAiModel { get; init; } = "gpt-4o-mini";

    /// <summary>
    /// Short on purpose. BR-REC-007 prefers a stale answer to a slow one, and a member staring at a
    /// spinner has already had the worse experience whatever arrives afterwards.
    /// </summary>
    [Range(1, 60)]
    public int TimeoutSeconds { get; init; } = 12;
}
