using Astrolabe.Application.Contracts.Mail;
using Astrolabe.Domain.Features.Identity.ValueObjects;
using Microsoft.Extensions.Options;

namespace Astrolabe.Application.Shared.Mail;

/// <summary>The copy of every email the network domain sends.</summary>
public sealed class NetworkMailTemplates(IOptions<MailOptions> options)
{
    private readonly string _baseUrl = options.Value.FrontendBaseUrl.TrimEnd('/');

    public EmailMessage BuildAdminInvitation(
        Email recipient, string fullName, string token, string? message) =>
        new()
        {
            ToAddress = recipient.Value,
            ToDisplayName = fullName,
            Subject = "You have been invited to administer Astrolabe Books",
            TextBody =
                $"""
                 Hello {fullName},

                 You have been invited to administer libraries on Astrolabe Books.
                 {(string.IsNullOrWhiteSpace(message) ? string.Empty : $"\n{message.Trim()}\n")}
                 Accept the invitation and choose your password here:

                 {_baseUrl}/accept-invitation?token={Uri.EscapeDataString(token)}

                 This link works once and expires in 7 days. You gain no access until you use it.

                 Astrolabe Books
                 """
        };
}
