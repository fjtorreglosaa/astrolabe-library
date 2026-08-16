using Astrolabe.Application.Abstractions.Mail;
using Astrolabe.Application.Contracts.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RestSharp;
using RestSharp.Authenticators;

namespace Astrolabe.Infrastructure.Integrations.Mail;

/// <summary>
/// Sends transactional email through the Mailgun HTTP API.
///
/// The client is a singleton: RestSharp wraps HttpClient, and constructing one per message would
/// exhaust sockets under load.
///
/// A provider failure never throws. Registration and password recovery decide for themselves how to
/// react to an undelivered message, so the outcome is returned as a value.
/// </summary>
public sealed class MailgunEmailSender : IEmailSender, IDisposable
{
    private readonly RestClient _client;
    private readonly MailgunOptions _options;
    private readonly ILogger<MailgunEmailSender> _logger;

    public MailgunEmailSender(IOptions<MailgunOptions> options, ILogger<MailgunEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;

        _client = new RestClient(new RestClientOptions(_options.BaseUrl)
        {
            Authenticator = new HttpBasicAuthenticator("api", _options.ApiKey),
            Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds)
        });
    }

    public async Task<EmailDeliveryResult> SendAsync(
        EmailMessage message,
        CancellationToken cancellationToken = default)
    {
        var request = new RestRequest($"/v3/{_options.Domain}/messages", Method.Post)
        {
            AlwaysMultipartFormData = true
        };

        request.AddParameter("from", $"{_options.FromDisplayName} <{_options.FromAddress}>");
        request.AddParameter("to", message.FormattedRecipient);
        request.AddParameter("subject", message.Subject);
        request.AddParameter("text", message.TextBody);

        if (!string.IsNullOrWhiteSpace(message.HtmlBody))
        {
            request.AddParameter("html", message.HtmlBody);
        }

        RestResponse response;

        try
        {
            response = await _client.ExecuteAsync(request, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // A transport fault is an infrastructure problem, not a business outcome.
            _logger.LogError(exception, "Mailgun request failed before a response was received.");
            return EmailDeliveryResult.Failure("The email provider could not be reached.");
        }

        if (response.IsSuccessful)
        {
            var messageId = ExtractMessageId(response.Content);

            _logger.LogInformation(
                "Email accepted by Mailgun. Subject: {Subject}, ProviderMessageId: {ProviderMessageId}",
                message.Subject,
                messageId);

            return EmailDeliveryResult.Success(messageId);
        }

        // The recipient address is deliberately absent from the log: it is personal data, and the
        // provider message identifier is enough to trace a delivery.
        _logger.LogError(
            "Mailgun rejected a message. Status: {Status}, Subject: {Subject}, Body: {Body}{SandboxHint}",
            (int)response.StatusCode,
            message.Subject,
            response.Content,
            _options.IsSandbox
                ? " (sandbox domain: the recipient must be an authorised recipient in Mailgun)"
                : string.Empty);

        return EmailDeliveryResult.Failure($"Mailgun returned {(int)response.StatusCode}.");
    }

    /// <summary>
    /// Pulls the identifier out of Mailgun's response without deserialising into a typed model,
    /// which would tie the abstraction to a provider-specific contract for one field.
    /// </summary>
    private static string? ExtractMessageId(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        const string marker = "\"id\":";
        var start = content.IndexOf(marker, StringComparison.Ordinal);

        if (start < 0)
        {
            return null;
        }

        var openingQuote = content.IndexOf('"', start + marker.Length);

        if (openingQuote < 0)
        {
            return null;
        }

        var closingQuote = content.IndexOf('"', openingQuote + 1);

        return closingQuote < 0 ? null : content[(openingQuote + 1)..closingQuote];
    }

    public void Dispose() => _client.Dispose();
}
