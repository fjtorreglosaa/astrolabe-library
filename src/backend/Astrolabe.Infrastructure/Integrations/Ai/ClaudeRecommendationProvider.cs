using System.Text.Json;
using Astrolabe.Application.Abstractions.Recommendations;
using Astrolabe.Application.Contracts.Recommendations;
using Astrolabe.Domain.Features.Recommendations.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RestSharp;

namespace Astrolabe.Infrastructure.Integrations.Ai;

/// <summary>
/// The Anthropic Messages API. Implements <see cref="IAiRecommendationProvider"/> for
/// <see cref="AiProvider.Claude"/>.
///
/// <para>
/// Nothing here throws. BR-REC-007 says a member must never see an error on this surface, and a
/// client that throws makes that every caller's problem — including the callers written later by
/// somebody who has not read the rule.
/// </para>
/// </summary>
public sealed class ClaudeRecommendationProvider(
    IOptions<AiProviderOptions> options,
    ILogger<ClaudeRecommendationProvider> logger) : IAiRecommendationProvider, IDisposable
{
    private readonly AiProviderOptions _options = options.Value;

    private readonly RestClient _client = new(new RestClientOptions(options.Value.ClaudeBaseUrl)
    {
        Timeout = TimeSpan.FromSeconds(options.Value.TimeoutSeconds),
    });

    public AiProvider Provider => AiProvider.Claude;

    /// <summary>
    /// BR-REC-008. The smallest call the vendor accepts: one token of output is enough to learn
    /// whether the key works, and costs the library almost nothing to find out.
    /// </summary>
    public async Task<bool> VerifyCredentialAsync(
        string credential, CancellationToken cancellationToken = default)
    {
        var request = Build(credential, "ping", maxTokens: 1);

        var response = await _client.ExecutePostAsync(request, cancellationToken);

        if (!response.IsSuccessful)
        {
            logger.LogWarning(
                "Claude refused a credential verification with {Status}.", response.StatusCode);
        }

        return response.IsSuccessful;
    }

    public async Task<IReadOnlyList<ProviderSuggestion>> SuggestAsync(
        string credential, ReadingProfile profile, int count,
        CancellationToken cancellationToken = default)
    {
        var request = Build(credential, AiPrompt.Build(profile, count), maxTokens: 1024);

        var response = await _client.ExecutePostAsync(request, cancellationToken);

        if (!response.IsSuccessful || response.Content is null)
        {
            // Empty, not an exception: the generator reads an empty list as a failure and falls
            // back, which is the single place that decision belongs.
            logger.LogWarning("Claude did not answer: {Status}.", response.StatusCode);
            return [];
        }

        return AiPrompt.Parse(ExtractText(response.Content));
    }

    private RestRequest Build(string credential, string prompt, int maxTokens)
    {
        var request = new RestRequest("/v1/messages");

        // The credential goes on the wire and nowhere else. It is never logged, never stored here,
        // and never returned — it arrived decrypted one call ago and ends its life in this header.
        request.AddHeader("x-api-key", credential);
        request.AddHeader("anthropic-version", "2023-06-01");
        request.AddJsonBody(new
        {
            model = _options.ClaudeModel,
            max_tokens = maxTokens,
            messages = new[] { new { role = "user", content = prompt } },
        });

        return request;
    }

    /// <summary>
    /// Pulls the text out of the content blocks. Returns null on anything unexpected, which the
    /// parser reads as "no suggestions" and the generator reads as a failure.
    /// </summary>
    private static string? ExtractText(string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);

            if (!document.RootElement.TryGetProperty("content", out var content))
            {
                return null;
            }

            return content.EnumerateArray()
                .Where(block => block.TryGetProperty("text", out _))
                .Select(block => block.GetProperty("text").GetString())
                .FirstOrDefault(text => !string.IsNullOrWhiteSpace(text));
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public void Dispose() => _client.Dispose();
}
