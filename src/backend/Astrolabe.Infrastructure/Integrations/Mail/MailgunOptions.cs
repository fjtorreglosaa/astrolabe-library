using System.ComponentModel.DataAnnotations;

namespace Astrolabe.Infrastructure.Integrations.Mail;

/// <summary>
/// Mailgun configuration, bound with the Options Pattern and validated at startup so a missing key
/// fails the process immediately rather than the first time someone registers.
/// </summary>
public sealed class MailgunOptions
{
    public const string SectionName = "Mailgun";

    /// <summary>
    /// Mailgun API key. Supplied by environment variable only — never committed.
    /// Note the region: EU accounts use https://api.eu.mailgun.net.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string ApiKey { get; init; } = string.Empty;

    /// <summary>The sending domain, for example <c>sandbox....mailgun.org</c>.</summary>
    [Required(AllowEmptyStrings = false)]
    public string Domain { get; init; } = string.Empty;

    [Required, Url]
    public string BaseUrl { get; init; } = "https://api.mailgun.net";

    /// <summary>The address messages are sent from. Must belong to <see cref="Domain"/>.</summary>
    [Required, EmailAddress]
    public string FromAddress { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string FromDisplayName { get; init; } = "Astrolabe Books";

    /// <summary>
    /// Mailgun sandbox domains only deliver to pre-authorised recipients. Setting this to true makes
    /// that constraint explicit in logs, so a rejected recipient reads as a configuration limit
    /// rather than a bug.
    /// </summary>
    public bool IsSandbox { get; init; } = true;

    public int TimeoutSeconds { get; init; } = 30;
}
