using System.Text;
using System.Text.Json;
using Astrolabe.Application.Contracts.Recommendations;

namespace Astrolabe.Infrastructure.Integrations.Ai;

/// <summary>
/// Builds the prompt and reads the answer. Shared by both vendors, because the ask is the same and
/// only the transport differs.
///
/// <para>
/// The prompt is assembled from a <see cref="ReadingProfile"/> and nothing else, which is how
/// BR-REC-005 survives contact with a string builder — there is no parameter here that could carry
/// a name or an address even by accident.
/// </para>
/// </summary>
public static class AiPrompt
{
    public static string Build(ReadingProfile profile, int count)
    {
        var builder = new StringBuilder();

        builder.AppendLine(
            "You recommend books from a fixed catalogue. Choose only from the candidates listed.");
        builder.AppendLine(
            $"Return exactly {count} suggestions as a JSON array, each object having bookId, "
            + "reason and matchPercent.");
        builder.AppendLine(
            "The reason must be one sentence explaining the choice from the reader's history. "
            + "Never return a suggestion without a reason.");
        builder.AppendLine();
        builder.AppendLine($"Genres this reader borrows, most frequent first: {string.Join(", ", profile.Genres)}");
        builder.AppendLine($"Titles they read recently: {string.Join("; ", profile.RecentTitles)}");
        builder.AppendLine();
        builder.AppendLine("Candidates:");

        foreach (var candidate in profile.CandidateBookIds)
        {
            builder.AppendLine($"{candidate.BookId} | {candidate.Title} | {candidate.Author} | {candidate.Genre}");
        }

        return builder.ToString();
    }

    /// <summary>
    /// Reads whatever came back, and returns nothing rather than throwing on anything unexpected.
    ///
    /// A vendor answering with prose around its JSON, a truncated array or a changed field name is a
    /// provider failure, and BR-REC-007 makes every provider failure a fallback rather than an
    /// error. Parsing is therefore deliberately forgiving in what it accepts and silent in what it
    /// rejects.
    /// </summary>
    public static IReadOnlyList<ProviderSuggestion> Parse(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return [];
        }

        var start = content.IndexOf('[');
        var end = content.LastIndexOf(']');

        if (start < 0 || end <= start)
        {
            return [];
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<List<Suggestion>>(
                content[start..(end + 1)],
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (parsed is null)
            {
                return [];
            }

            return [.. parsed
                .Where(suggestion => Guid.TryParse(suggestion.BookId, out _))
                .Select(suggestion => new ProviderSuggestion(
                    Guid.Parse(suggestion.BookId!), suggestion.Reason ?? string.Empty,
                    suggestion.MatchPercent))];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private sealed record Suggestion(string? BookId, string? Reason, int MatchPercent);
}
