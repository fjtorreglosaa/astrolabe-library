using Astrolabe.Application.Abstractions.Mail;
using Astrolabe.Application.Contracts.Mail;
using Astrolabe.Infrastructure.Integrations.Mail;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Astrolabe.Infrastructure.Tests.Integrations.Mail;

/// <summary>
/// Covers the Mailgun sender against a stubbed HTTP server. No real network call is made, per
/// SDD_PLIUS_STRATEGY.md section 9.1.
/// </summary>
[TestFixture]
public sealed class MailgunEmailSenderTests
{
    private const string Domain = "sandbox.example.org";

    private WireMockServer _server = null!;

    [SetUp]
    public void SetUp() => _server = WireMockServer.Start();

    [TearDown]
    public void TearDown() => _server.Dispose();

    private MailgunEmailSender CreateSender() => new(
        Options.Create(new MailgunOptions
        {
            ApiKey = "key-test",
            Domain = Domain,
            BaseUrl = _server.Url!,
            FromAddress = $"postmaster@{Domain}",
            FromDisplayName = "Astrolabe Books",
            IsSandbox = true,
            TimeoutSeconds = 10
        }),
        NullLogger<MailgunEmailSender>.Instance);

    private static EmailMessage AMessage() => new()
    {
        ToAddress = "member@example.com",
        ToDisplayName = "Ada Lovelace",
        Subject = "Verify your account",
        TextBody = "Open the link to activate your account."
    };

    private void StubMessages(int statusCode, string body) =>
        _server
            .Given(Request.Create().WithPath($"/v3/{Domain}/messages").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(statusCode).WithBody(body));

    [Test]
    public async Task SendAsync_WhenMailgunAccepts_ReturnsAcceptedWithProviderMessageId()
    {
        StubMessages(200, """{"id":"<20260816.abc@sandbox.example.org>","message":"Queued. Thank you."}""");
        var sender = CreateSender();

        var result = await sender.SendAsync(AMessage(), TestContext.CurrentContext.CancellationToken);

        result.Accepted.Should().BeTrue();
        result.ProviderMessageId.Should().Be("<20260816.abc@sandbox.example.org>");
        result.FailureReason.Should().BeNull();
    }

    [Test]
    public async Task SendAsync_PostsToTheConfiguredDomainEndpoint()
    {
        StubMessages(200, """{"id":"<x@y>"}""");
        var sender = CreateSender();

        await sender.SendAsync(AMessage(), TestContext.CurrentContext.CancellationToken);

        var request = _server.LogEntries.Single().RequestMessage;
        request.Should().NotBeNull();
        request!.Path.Should().Be($"/v3/{Domain}/messages");
        request.Method.Should().Be("POST");
    }

    [Test]
    public async Task SendAsync_AuthenticatesWithBasicAuth()
    {
        // Mailgun expects the literal user "api" and the key as the password.
        StubMessages(200, """{"id":"<x@y>"}""");
        var sender = CreateSender();

        await sender.SendAsync(AMessage(), TestContext.CurrentContext.CancellationToken);

        var request = _server.LogEntries.Single().RequestMessage;
        request.Should().NotBeNull();
        request!.Headers.Should().NotBeNull().And.ContainKey("Authorization");
        request.Headers!["Authorization"].ToString().Should().StartWith("Basic ");
    }

    [Test]
    public async Task SendAsync_WhenMailgunRejects_ReturnsFailureWithoutThrowing()
    {
        // A sandbox domain answers 400 for an unauthorised recipient. That must surface as a value,
        // not an exception, so the caller can decide what to tell the user.
        StubMessages(400, """{"message":"to parameter is not a valid address"}""");
        var sender = CreateSender();

        var result = await sender.SendAsync(AMessage(), TestContext.CurrentContext.CancellationToken);

        result.Accepted.Should().BeFalse();
        result.FailureReason.Should().Contain("400");
        result.ProviderMessageId.Should().BeNull();
    }

    [Test]
    public async Task SendAsync_WhenMailgunIsUnreachable_ReturnsFailure()
    {
        var sender = new MailgunEmailSender(
            Options.Create(new MailgunOptions
            {
                ApiKey = "key-test",
                Domain = Domain,
                // A port nothing is listening on.
                BaseUrl = "http://127.0.0.1:1",
                FromAddress = $"postmaster@{Domain}",
                FromDisplayName = "Astrolabe Books",
                TimeoutSeconds = 2
            }),
            NullLogger<MailgunEmailSender>.Instance);

        var result = await sender.SendAsync(AMessage(), TestContext.CurrentContext.CancellationToken);

        result.Accepted.Should().BeFalse();
        result.FailureReason.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task SendAsync_WhenResponseHasNoIdentifier_StillReportsAccepted()
    {
        StubMessages(200, """{"message":"Queued. Thank you."}""");
        var sender = CreateSender();

        var result = await sender.SendAsync(AMessage(), TestContext.CurrentContext.CancellationToken);

        result.Accepted.Should().BeTrue();
        result.ProviderMessageId.Should().BeNull();
    }

    [Test]
    public void FormattedRecipient_IncludesTheDisplayNameWhenPresent()
    {
        AMessage().FormattedRecipient.Should().Be("Ada Lovelace <member@example.com>");
    }

    [Test]
    public void FormattedRecipient_FallsBackToTheBareAddress()
    {
        var message = new EmailMessage
        {
            ToAddress = "member@example.com",
            Subject = "s",
            TextBody = "t"
        };

        message.FormattedRecipient.Should().Be("member@example.com");
    }
}
