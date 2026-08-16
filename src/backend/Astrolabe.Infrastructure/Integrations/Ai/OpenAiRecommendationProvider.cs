using System.Text.Json;
using Astrolabe.Application.Abstractions.Recommendations;
using Astrolabe.Application.Contracts.Recommendations;
using Astrolabe.Domain.Features.Recommendations.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RestSharp;

namespace Astrolabe.Infrastructure.Integrations.Ai;

/// <summary>
/// The OpenAI Chat Completions API. Implements <see cref="IAiRecommendationProvider"/> for
/// <see cref="AiProvider.OpenAI"/>.
///
/// Same contract as the Claude client and the same silence on failure, for the same reason: the
/// decision to fall back belongs in one place, and it is not here.
/// </summary>
public sealed class OpenAiRecommendationProvider(
    IOptions<AiProviderOptions> options,
    ILogger<OpenAiRecommendationProvider> logger) : IAiRecommendationProvider, IDisposable
{
    private readonly AiProviderOptions _options = options.Value;

    private readonly RestClient _client = new(new RestClientOptions(options.Value.OpenAiBaseUrl)
    {
        Timeout = TimeSpan.FromSeconds(options.Value.TimeoutSeconds),
    });

    public AiProvider Provider => AiProvider.OpenAI;

    public async Task<bool> VerifyCredentialAsync(
        string credential, CancellationToken cancellationToken = default)
    {
        // Listing models is the cheapest call that proves a key works, and unlike a completion it
        // costs the library nothing at all.
        var request = new RestRequest("/v1/models");
        request.AddHeader("Authorization", $"Bearer {credential}");

        var response = await _client.ExecuteGetAsync(request, cancellationToken);

        if (!response.IsSuccessful)
        {
            logger.LogWarning(
                "OpenAI refused a credential verification with {Status}.", response.StatusCode);
        }

        return response.IsSuccessful;
    }

    public async Task<IReadOnlyList<ProviderSuggestion>> SuggestAsync(
        string credential, ReadingProfile profile, int count,
        CancellationToken cancellationToken = default)
    {
        var request = new RestRequest("/v1/chat/completions");
        request.AddHeader("Authorization", $"Bearer {credential}");
        request.AddJsonBody(new
        {
            model = _options.OpenAiModel,
            messages = new[]
            {
                new { role = "user", content = AiPrompt.Build(profile, count) },
            },
        });

        var response = await _client.ExecutePostAsync(request, cancellationToken);

        if (!response.IsSuccessful || response.Content is null)
        {
            logger.LogWarning("OpenAI did not answer: {Status}.", response.StatusCode);
            return [];
        }

        return AiPrompt.Parse(ExtractText(response.Content));
    }

    private static string? ExtractText(string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);

            if (!document.RootElement.TryGetProperty("choices", out var choices))
            {
                return null;
            }

            return choices.EnumerateArray()
                .Select(choice => choice.TryGetProperty("message", out var message)
                    && message.TryGetProperty("content", out var content)
                        ? content.GetString()
                        : null)
                .FirstOrDefault(text => !string.IsNullOrWhiteSpace(text));
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public void Dispose() => _client.Dispose();
}
