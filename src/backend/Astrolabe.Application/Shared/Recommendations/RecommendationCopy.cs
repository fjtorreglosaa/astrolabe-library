using Astrolabe.Domain.Features.Recommendations.Enums;

namespace Astrolabe.Application.Shared.Recommendations;

/// <summary>
/// The wording the recommendations surface uses, transcribed from the prototype.
///
/// Server-side because the same sentence has to be right on every client, and because the choice of
/// sentence follows a rule the client cannot see — whether a library is connected is not something
/// the browser is told.
/// </summary>
public static class RecommendationCopy
{
    /// <summary>Shown with a model-generated set. The prototype's own sentence.</summary>
    public const string ModelNote =
        "Your library runs this on its own key. The model reads your reservation history and "
        + "preferred topics, and only suggests titles with copies in the catalogue.";

    /// <summary>Shown with the fallback. It explains itself rather than looking like a failure.</summary>
    public const string FallbackNote =
        "Your library has not connected a model yet, so these are the most borrowed titles in your "
        + "genres.";

    /// <summary>BR-REC-002. What a Basic member is told, which is not "no results".</summary>
    public const string PlanNote =
        "Personalised picks are part of the Plus and Max plans. Basic keeps full browsing and "
        + "reservations at your home library, without model-generated suggestions.";

    /// <summary>The reason attached to a fallback suggestion. BR-REC-010 exempts nothing.</summary>
    public const string FallbackReason = "One of the most borrowed titles in your genres.";

    public static string StatusFor(AiProvider? provider, bool isConnected) =>
        isConnected && provider is { } live ? $"{Label(live)} connected" : "Not configured";

    public static string NoteFor(bool isConnected) =>
        isConnected
            ? "Members of this library get AI suggestions."
            : "Members here see the non-AI fallback list.";

    /// <summary>The vendor's name as the prototype writes it. "Claude", never "Anthropic".</summary>
    public static string Label(AiProvider provider) => provider switch
    {
        AiProvider.OpenAI => "OpenAI",
        _ => "Claude"
    };
}
