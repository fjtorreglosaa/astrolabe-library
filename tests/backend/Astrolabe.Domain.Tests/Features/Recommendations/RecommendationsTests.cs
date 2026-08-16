using System.Text;
using System.Text.Json;
using Astrolabe.Domain.Features.Membership.Enums;
using Astrolabe.Domain.Features.Recommendations.Entities;
using Astrolabe.Domain.Features.Recommendations.Enums;
using Astrolabe.Domain.Features.Recommendations.Errors;
using Astrolabe.Domain.Features.Recommendations.Events;
using Astrolabe.Domain.Features.Recommendations.Policies;
using Astrolabe.Domain.Features.Recommendations.ValueObjects;
using FluentAssertions;

namespace Astrolabe.Domain.Tests.Features.Recommendations;

/// <summary>
/// Covers the recommendations domain: BR-REC-002 to BR-REC-012.
///
/// <para>
/// The credential tests carry the most weight. `BR-REC-004` is the rule this domain cannot afford to
/// break even once, and the defence is a type with no way back to plaintext rather than a promise
/// that no DTO will ever expose one.
/// </para>
/// </summary>
[TestFixture]
public sealed class RecommendationsTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 16, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid Library = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid MemberId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static EncryptedSecret ASecret(string cipher = "cipher-bytes") =>
        EncryptedSecret.Create(Encoding.UTF8.GetBytes(cipher), "v1").Value;

    private static LibraryAiConfiguration AConfiguration() =>
        LibraryAiConfiguration.Configure(Library, AiProvider.Claude, ASecret(), Now).Value;

    private static RecommendationItem AnItem(string reason = "Because you read two of these.") =>
        RecommendationItem.Create(Guid.NewGuid(), reason, 94).Value;

    // ---------- BR-REC-004: the credential cannot escape ----------

    [Test]
    public void ASecretRefusesToDescribeItself()
    {
        // The interpolated-string route out. A log line, an exception message or a debugger watch
        // must not become the way a provider key leaves the building.
        var secret = ASecret("sk-a-real-looking-key");

        secret.ToString().Should().NotContain("sk-a-real-looking-key");
        secret.ToString().Should().Contain("redacted");
    }

    [Test]
    public void ASecretCannotBeSerialisedIntoItsPlaintext()
    {
        // The accidental-DTO route out. Nothing on this type reads back as the key, so a record that
        // happened to carry a configuration could not leak one even if somebody wrote it.
        var json = JsonSerializer.Serialize(ASecret("sk-a-real-looking-key"));

        json.Should().NotContain("sk-a-real-looking-key");
    }

    [Test]
    public void ASecretHandsOutACopyOfItsCipherText()
    {
        // Otherwise a caller could mutate stored ciphertext through the array it was given.
        var secret = ASecret();
        var first = secret.CipherText;
        first[0] = 0xFF;

        secret.CipherText.Should().NotEqual(first);
    }

    [Test]
    public void AnEmptyCredentialIsRefused()
    {
        EncryptedSecret.Create([], "v1").Error.Should().Be(RecommendationErrors.CredentialEmpty);
    }

    [Test]
    public void ACredentialMustRecordWhichKeyEncryptedIt()
    {
        // Without it a key rotation would silently disconnect every library, because nothing would
        // know which key ring entry could decrypt an old row.
        EncryptedSecret.Create([1, 2, 3], "  ")
            .Error.Should().Be(RecommendationErrors.CredentialKeyVersionMissing);
    }

    // ---------- BR-REC-008: verified before live ----------

    [Test]
    public void ANewConfigurationIsNeitherVerifiedNorEnabled()
    {
        // The prototype's button is "Save and test", and until the test passes the library is
        // exactly as unconnected as it was.
        var configuration = AConfiguration();

        configuration.IsVerified.Should().BeFalse();
        configuration.IsEnabled.Should().BeFalse();
        configuration.IsConnected.Should().BeFalse();
    }

    [Test]
    public void AnUnverifiedConfigurationCannotBeEnabled()
    {
        AConfiguration().Enable()
            .Error.Should().Be(RecommendationErrors.CannotEnableAnUnverifiedCredential);
    }

    [Test]
    public void VerifyingThenEnablingConnectsTheLibrary()
    {
        var configuration = AConfiguration();
        configuration.MarkVerified(Now);

        configuration.Enable().IsSuccess.Should().BeTrue();
        configuration.IsConnected.Should().BeTrue();
    }

    // ---------- BR-REC-007 and BR-REC-012: failing and switching off differ ----------

    [Test]
    public void AProviderFailureDropsVerificationButNotTheLibrarysDecision()
    {
        // Staff need to know whether to fix a key or flip a switch they never touched. Dropping
        // IsEnabled here would tell them the wrong one.
        var configuration = AConfiguration();
        configuration.MarkVerified(Now);
        configuration.Enable();

        configuration.MarkFailed(Now.AddHours(1));

        configuration.IsEnabled.Should().BeTrue();
        configuration.IsVerified.Should().BeFalse();
        configuration.IsConnected.Should().BeFalse();
    }

    [Test]
    public void DisablingKeepsTheCredential()
    {
        // BR-REC-012 says so in as many words, and it is what lets a library switch back on without
        // paying to verify a key that was never in doubt.
        var configuration = AConfiguration();
        configuration.MarkVerified(Now);
        configuration.Enable();

        configuration.Disable(Now.AddHours(1));

        configuration.IsEnabled.Should().BeFalse();
        configuration.IsVerified.Should().BeTrue();
        configuration.Credential.Should().NotBeNull();
    }

    [Test]
    public void DisablingRaisesTheEventThatEvictsCachedSets()
    {
        // How "immediate" is enforced without every caller remembering it.
        var configuration = AConfiguration();
        configuration.MarkVerified(Now);
        configuration.Enable();

        configuration.Disable(Now.AddHours(1));

        configuration.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<LibraryAiDisabled>();
    }

    [Test]
    public void DisablingSomethingAlreadyOffRaisesNothing()
    {
        var configuration = AConfiguration();

        configuration.Disable(Now);

        configuration.DomainEvents.Should().BeEmpty();
    }

    [Test]
    public void ReplacingAKeyReturnsToUnverified()
    {
        // Including a replacement for one that worked: BR-REC-008 does not let an untested
        // credential go live, and "it is probably fine" is how a library stops answering silently.
        var configuration = AConfiguration();
        configuration.MarkVerified(Now);
        configuration.Enable();

        configuration.Replace(AiProvider.OpenAI, ASecret("another"), Now.AddDays(1));

        configuration.IsVerified.Should().BeFalse();
        configuration.IsConnected.Should().BeFalse();
        configuration.IsEnabled.Should().BeTrue("rotating a key is not changing your mind");
    }

    // ---------- BR-REC-010: every suggestion states why ----------

    [TestCase("")]
    [TestCase("   ")]
    public void ASuggestionWithoutAReasonIsRefused(string reason)
    {
        RecommendationItem.Create(Guid.NewGuid(), reason, 90)
            .Error.Should().Be(RecommendationErrors.ReasonRequired);
    }

    [Test]
    public void AMatchPercentIsClampedRatherThanRejected()
    {
        // A vendor answering 103 is a reason to show 100, not to discard an otherwise good
        // suggestion. The number is display copy, not a ranking — see the business spec, §8.
        RecommendationItem.Create(Guid.NewGuid(), "Any reason", 103).Value.MatchPercent.Should().Be(100);
        RecommendationItem.Create(Guid.NewGuid(), "Any reason", -5).Value.MatchPercent.Should().Be(0);
    }

    // ---------- BR-REC-006: sets are cached, with an expiry ----------

    [Test]
    public void AModelSetKnowsWhichLibraryPaidForIt()
    {
        // Stored so BR-REC-012 can evict exactly the sets a switched-off library generated.
        var set = RecommendationSet.FromModel(
            MemberId, Library, [AnItem()], Now, TimeSpan.FromHours(24)).Value;

        set.Source.Should().Be(RecommendationSource.Model);
        set.GeneratedByLibraryId.Should().Be(Library);
    }

    [Test]
    public void AFallbackSetCostsNobodyAnything()
    {
        var set = RecommendationSet.FromFallback(MemberId, [AnItem()], Now, TimeSpan.FromHours(24));

        set.Source.Should().Be(RecommendationSource.Fallback);
        set.GeneratedByLibraryId.Should().BeNull();
    }

    [Test]
    public void AnEmptyModelAnswerIsAFailureRatherThanAnEmptySet()
    {
        // So the caller falls back, which is what BR-REC-003 and BR-REC-007 both ask for. An empty
        // set would instead be cached and served as though it were an answer.
        RecommendationSet.FromModel(MemberId, Library, [], Now, TimeSpan.FromHours(24))
            .Error.Should().Be(RecommendationErrors.NothingToRecommend);
    }

    [Test]
    public void AFallbackCannotFail()
    {
        // It has no Result at all, on purpose: this is where every other path goes when it breaks,
        // so it must not be able to break itself.
        RecommendationSet.FromFallback(MemberId, [], Now, TimeSpan.FromHours(1))
            .Should().NotBeNull();
    }

    [Test]
    public void FreshnessIsDecidedByTheExpiry()
    {
        var set = RecommendationSet.FromFallback(MemberId, [AnItem()], Now, TimeSpan.FromHours(24));

        set.IsFresh(Now.AddHours(23)).Should().BeTrue();
        set.IsFresh(Now.AddHours(25)).Should().BeFalse();
    }

    // ---------- BR-REC-002 and BR-REC-003: who sees what ----------

    [Test]
    public void ABasicMemberNeverSeesTheSurface()
    {
        // Not even the fallback. A Basic member is not a member whose library happens to be
        // unconnected, and serving them the most-borrowed list would quietly hand them a benefit
        // their plan excludes while explaining nothing.
        RecommendationAccessPolicy.Evaluate(PlanTier.Basic, connectedLibrariesInCity: 3)
            .Should().Be(RecommendationVerdict.NotIncludedInPlan);
    }

    [TestCase(PlanTier.Plus)]
    [TestCase(PlanTier.Max)]
    public void APaidMemberWithAConnectedLibraryGetsAModelAnswer(PlanTier plan)
    {
        RecommendationAccessPolicy.Evaluate(plan, connectedLibrariesInCity: 1)
            .Should().Be(RecommendationVerdict.ModelGenerated);
    }

    [TestCase(PlanTier.Plus)]
    [TestCase(PlanTier.Max)]
    public void APaidMemberWithNoConnectedLibraryGetsTheFallback(PlanTier plan)
    {
        // Never an error, per BR-REC-003. The member did nothing wrong and neither did the product.
        RecommendationAccessPolicy.Evaluate(plan, connectedLibrariesInCity: 0)
            .Should().Be(RecommendationVerdict.Fallback);
    }

    [Test]
    public void OneConnectedLibraryInTheCityIsEnough()
    {
        // The prototype filters live libraries by city rather than by home library, so a member is
        // not denied their own city's connected branch because the nearest one has not paid.
        RecommendationAccessPolicy.Evaluate(PlanTier.Plus, connectedLibrariesInCity: 1)
            .Should().Be(RecommendationVerdict.ModelGenerated);
    }
}
