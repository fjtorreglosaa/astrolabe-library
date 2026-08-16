using Astrolabe.Application.Contracts.Mail;
using Astrolabe.Domain.Features.Identity.ValueObjects;
using Microsoft.Extensions.Options;

namespace Astrolabe.Application.Shared.Mail;

/// <summary>
/// The copy of every email the identity domain sends.
///
/// <para>
/// Mail composition is a concern, not a domain, so it lives beside the other mail templates rather
/// than inside <c>Features/Identity</c>. Keeping it there would put a fourth kind of folder next to
/// Commands, Queries and Services, and would duplicate <see cref="MailOptions"/> once per domain
/// that sends email.
/// </para>
///
/// <para>
/// It is still application policy — what the message says and where it points — not infrastructure,
/// so swapping the provider must not rewrite a single word of copy.
/// </para>
/// </summary>
public sealed class IdentityMailTemplates(IOptions<MailOptions> options)
{
    private readonly string _baseUrl = options.Value.FrontendBaseUrl.TrimEnd('/');

    public EmailMessage BuildVerification(Email recipient, string fullName, string token) =>
        new()
        {
            ToAddress = recipient.Value,
            ToDisplayName = fullName,
            Subject = "Confirm your Astrolabe Books account",
            TextBody =
                $"""
                 Hello {fullName},

                 Confirm your email address to start borrowing:

                 {Link("verify", token)}

                 This link works once and expires in 24 hours.

                 If you did not create an account, ignore this message.

                 Astrolabe Books
                 """
        };

    public EmailMessage BuildPasswordRecovery(Email recipient, string fullName, string token) =>
        new()
        {
            ToAddress = recipient.Value,
            ToDisplayName = fullName,
            Subject = "Reset your Astrolabe Books password",
            TextBody =
                $"""
                 Hello {fullName},

                 Use this link to choose a new password:

                 {Link("reset-password", token)}

                 This link works once and expires in 1 hour.

                 If you did not ask to reset your password, ignore this message. Your password has
                 not changed, and you may want to review your active devices in Settings.

                 Astrolabe Books
                 """
        };

    /// <summary>
    /// Sent to an address that is already registered when someone tries to register it again.
    ///
    /// BR-IDN-030 forbids telling the person at the keyboard that the address exists, so the
    /// warning goes to the address itself: the account holder learns of the attempt, and the
    /// attacker learns nothing.
    /// </summary>
    public EmailMessage BuildDuplicateRegistrationNotice(Email recipient, string fullName) =>
        new()
        {
            ToAddress = recipient.Value,
            ToDisplayName = fullName,
            Subject = "Someone tried to register with your email address",
            TextBody =
                $"""
                 Hello {fullName},

                 Someone just tried to create an Astrolabe Books account with this email address.
                 You already have one, so nothing was created and nothing has changed.

                 If that was you, sign in instead:

                 {_baseUrl}/login

                 If it was not, you can reset your password here:

                 {_baseUrl}/forgot-password

                 Astrolabe Books
                 """
        };

    /// <summary>
    /// The token travels in the query string, which is unavoidable for a link in an email. It is
    /// single-use and short-lived precisely because that placement leaks it into browser history
    /// and referrer headers.
    /// </summary>
    private string Link(string path, string token) =>
        $"{_baseUrl}/{path}?token={Uri.EscapeDataString(token)}";
}
