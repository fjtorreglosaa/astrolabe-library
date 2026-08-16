using System.ComponentModel.DataAnnotations;

namespace Astrolabe.Application.Shared.Mail;

/// <summary>
/// Settings shared by every transactional email the system sends.
///
/// Deliberately not per domain: identity, network and billing all need the same frontend origin, so
/// three copies of it would be three chances to configure one of them wrongly.
/// </summary>
public sealed class MailOptions
{
    public const string SectionName = "Mail";

    [Required, Url]
    public string FrontendBaseUrl { get; init; } = "http://localhost:5173";
}
