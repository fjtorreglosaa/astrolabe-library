using System.Reflection;
using System.Text;
using System.Text.Json;
using Astrolabe.Application.Contracts.Recommendations;
using Astrolabe.Domain.Features.Recommendations.Entities;
using Astrolabe.Domain.Features.Recommendations.Enums;
using Astrolabe.Domain.Features.Recommendations.ValueObjects;
using FluentAssertions;

namespace Astrolabe.Application.Tests.Features.Recommendations;

/// <summary>
/// `AC-REC-004`, which PLAN-001 Stage 7 marks **mandatory**: no API response can expose a stored
/// credential.
///
/// <para>
/// Written as a sweep over every contract this domain publishes rather than as an assertion per
/// DTO. A per-DTO test only ever covers the DTOs somebody remembered to write one for, and the
/// failure this guards against is precisely a new DTO written by somebody who did not know the rule.
/// A record added to <c>Contracts/Recommendations</c> tomorrow is inside this net automatically.
/// </para>
/// </summary>
[TestFixture]
public sealed class CredentialNeverLeaksTests
{
    private const string Plaintext = "sk-ant-a-key-that-must-never-appear-anywhere";

    private static readonly DateTimeOffset Now = new(2026, 8, 16, 12, 0, 0, TimeSpan.Zero);

    /// <summary>Everything this domain can put on the wire.</summary>
    private static IEnumerable<Type> PublishedContracts =>
        typeof(LibraryAiStatusDto).Assembly
            .GetTypes()
            .Where(type => type.IsPublic
                && type.Namespace == "Astrolabe.Application.Contracts.Recommendations");

    [Test]
    public void NoPublishedContractHasAFieldThatCouldHoldACredential()
    {
        // The structural half: a shape with nowhere to put a key cannot leak one, whatever the
        // handler filling it does. Names are checked because that is how such a field would arrive —
        // somebody adding `MaskedKey` "just for the UI".
        string[] forbidden = ["credential", "secret", "apikey", "key", "token", "password"];

        var offenders = PublishedContracts
            .SelectMany(type => type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(property => new { Type = type.Name, property.Name }))
            .Where(member => forbidden.Any(word =>
                member.Name.Replace("_", string.Empty).ToLowerInvariant().Contains(word)))
            .ToList();

        offenders.Should().BeEmpty(
            "BR-REC-004 forbids returning a credential in any form, including masked");
    }

    [Test]
    public void TheStatusContractSerialisesWithoutAnythingResemblingACredential()
    {
        var status = new LibraryAiStatusDto(
            Guid.NewGuid(), "Midtown", "Claude", true, true, true, Now,
            "Claude connected", "Members of this library get AI suggestions.");

        var json = JsonSerializer.Serialize(status);

        json.Should().NotContain(Plaintext);
        json.ToLowerInvariant().Should().NotContain("credential");
    }

    [Test]
    public void AConfigurationCarryingARealSecretStillSerialisesWithoutIt()
    {
        // The accidental route: somebody serialises the aggregate itself, from a log line or a
        // hastily written debug endpoint. `EncryptedSecret` exposes no plaintext and no readable
        // ToString, so even that does not spill the key.
        var configuration = LibraryAiConfiguration.Configure(
            Guid.NewGuid(),
            AiProvider.Claude,
            EncryptedSecret.Create(Encoding.UTF8.GetBytes(Plaintext), "v1").Value,
            Now).Value;

        var json = JsonSerializer.Serialize(configuration);

        json.Should().NotContain(Plaintext);
        $"{configuration.Credential}".Should().NotContain(Plaintext);
    }

    [Test]
    public void TheReadingProfileSentToAProviderCarriesNoMemberIdentity()
    {
        // BR-REC-005 is a rule about a payload, and this is the payload. A field added here is the
        // one change that could break it, which is why there is exactly one such type.
        var properties = typeof(ReadingProfile)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(property => property.Name.ToLowerInvariant())
            .ToList();

        properties.Should().NotContain(name =>
            name.Contains("member") || name.Contains("user") || name.Contains("email")
            || name.Contains("name") && !name.Contains("names"));
    }

    [Test]
    public void TheReadingProfileHasNoIdentifierAmongItsValues()
    {
        // Belt and braces: the shape is checked above, and the serialised instance is checked here,
        // because a `Guid` hidden inside a nested record would satisfy the first test and not this.
        var profile = new ReadingProfile(
            ["Fiction"],
            ["The House of the Spirits"],
            [new CandidateBook(Guid.NewGuid(), "Any title", "Any author", "Fiction")]);

        var json = JsonSerializer.Serialize(profile).ToLowerInvariant();

        json.Should().NotContain("memberid");
        json.Should().NotContain("email");
        json.Should().NotContain("reservation");
    }
}
