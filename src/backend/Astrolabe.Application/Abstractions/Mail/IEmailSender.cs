using Astrolabe.Application.Contracts.Mail;

namespace Astrolabe.Application.Abstractions.Mail;

/// <summary>
/// Sends transactional email. Declared in the Application layer so use cases depend on the
/// capability, never on Mailgun or any other provider. The implementation lives in Infrastructure.
/// </summary>
public interface IEmailSender
{
    Task<EmailDeliveryResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
