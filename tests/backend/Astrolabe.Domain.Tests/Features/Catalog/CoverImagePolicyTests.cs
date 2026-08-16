using Astrolabe.Domain.Features.Catalog.Entities;
using Astrolabe.Domain.Features.Catalog.Errors;
using Astrolabe.Domain.Features.Catalog.Policies;
using FluentAssertions;

namespace Astrolabe.Domain.Tests.Features.Catalog;

/// <summary>
/// Covers what may be accepted as a cover image. Backs BR-CAT-005's other half — a book has either a
/// real cover or a generated tint, and a broken image is neither.
/// </summary>
[TestFixture]
public sealed class CoverImagePolicyTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 16, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid BookId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static byte[] Jpeg(int size = 64)
    {
        var bytes = new byte[size];
        bytes[0] = 0xFF; bytes[1] = 0xD8; bytes[2] = 0xFF;
        return bytes;
    }

    private static byte[] Png(int size = 64)
    {
        var bytes = new byte[size];
        byte[] signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        signature.CopyTo(bytes, 0);
        return bytes;
    }

    private static byte[] Webp(int size = 64)
    {
        var bytes = new byte[size];
        "RIFF"u8.ToArray().CopyTo(bytes, 0);
        "WEBP"u8.ToArray().CopyTo(bytes, 8);
        return bytes;
    }

    [Test]
    public void AJpegIsAccepted() =>
        CoverImagePolicy.EnsureAcceptable("image/jpeg", Jpeg()).IsSuccess.Should().BeTrue();

    [Test]
    public void APngIsAccepted() =>
        CoverImagePolicy.EnsureAcceptable("image/png", Png()).IsSuccess.Should().BeTrue();

    [Test]
    public void AWebpIsAccepted() =>
        CoverImagePolicy.EnsureAcceptable("image/webp", Webp()).IsSuccess.Should().BeTrue();

    [Test]
    public void TheContentTypeIsMatchedCaseInsensitively()
    {
        // Browsers are not consistent about this, and refusing "IMAGE/JPEG" would be theatre.
        CoverImagePolicy.EnsureAcceptable("IMAGE/JPEG", Jpeg()).IsSuccess.Should().BeTrue();
    }

    // ---------- The check that matters ----------

    [Test]
    public void AFileClaimingToBeAPngButIsNotIsRefused()
    {
        // The whole reason the bytes are read. A browser reports whatever type the operating system
        // guessed from an extension, so the declared type is a hint and the signature is evidence.
        // Storing this would mean later serving it to a browser under an image content type.
        CoverImagePolicy.EnsureAcceptable("image/png", "<html>hello</html>"u8.ToArray())
            .Error.Should().Be(CatalogErrors.CoverImageNotAnImage);
    }

    [Test]
    public void AJpegRenamedAsAPngIsRefused()
    {
        // Real bytes, wrong declaration. Refused rather than corrected: guessing what somebody meant
        // is how a content type stops meaning anything.
        CoverImagePolicy.EnsureAcceptable("image/png", Jpeg())
            .Error.Should().Be(CatalogErrors.CoverImageNotAnImage);
    }

    [Test]
    public void ATruncatedFileIsRefusedRatherThanReadPastItsEnd()
    {
        // A three-byte "PNG" would index past the end of the array if the length were not checked
        // before the signature.
        CoverImagePolicy.EnsureAcceptable("image/png", [0x89, 0x50, 0x4E])
            .Error.Should().Be(CatalogErrors.CoverImageNotAnImage);
    }

    // ---------- Type and size ----------

    [TestCase("image/gif")]
    [TestCase("image/svg+xml")]
    [TestCase("application/pdf")]
    [TestCase("text/html")]
    public void AnUnsupportedTypeIsRefused(string contentType)
    {
        // SVG in particular: it is an image by name and a script host by nature.
        CoverImagePolicy.EnsureAcceptable(contentType, Png())
            .Error.Should().Be(CatalogErrors.CoverImageTypeNotAllowed);
    }

    [Test]
    public void AnOversizedImageIsRefused()
    {
        CoverImagePolicy.EnsureAcceptable("image/jpeg", Jpeg(CoverImagePolicy.MaxBytes + 1))
            .Error.Should().Be(CatalogErrors.CoverImageTooLarge);
    }

    [Test]
    public void ExactlyTheLimitIsAccepted()
    {
        CoverImagePolicy.EnsureAcceptable("image/jpeg", Jpeg(CoverImagePolicy.MaxBytes))
            .IsSuccess.Should().BeTrue();
    }

    [TestCase(null)]
    [TestCase(new byte[0])]
    public void NothingAtAllIsRefusedAsEmpty(byte[]? content)
    {
        CoverImagePolicy.EnsureAcceptable("image/jpeg", content)
            .Error.Should().Be(CatalogErrors.CoverImageEmpty);
    }

    // ---------- The entity applies the same policy ----------

    [Test]
    public void TheEntityRefusesWhatThePolicyRefuses()
    {
        // Checked in the entity as well as at the edge, so no route into storage can skip it.
        BookCoverImage.Create(BookId, "image/png", "not a png"u8.ToArray(), Now)
            .Error.Should().Be(CatalogErrors.CoverImageNotAnImage);
    }

    [Test]
    public void ReplacingKeepsTheSameRowAndTheSameRules()
    {
        // One cover row per book for its whole life, so the URL pointing at it never has to change.
        var cover = BookCoverImage.Create(BookId, "image/png", Png(), Now).Value;

        cover.Replace("image/jpeg", Jpeg(), Now.AddDays(1)).IsSuccess.Should().BeTrue();
        cover.ContentType.Should().Be("image/jpeg");
        cover.UploadedAt.Should().Be(Now.AddDays(1));

        cover.Replace("image/png", "rubbish"u8.ToArray(), Now.AddDays(2))
            .Error.Should().Be(CatalogErrors.CoverImageNotAnImage);
        cover.ContentType.Should().Be("image/jpeg", "a refused replacement changes nothing");
    }

    [Test]
    public void TheContentTypeIsStoredLowercase()
    {
        // It goes straight into an HTTP response header, and a consistent casing there is one less
        // thing for a cache or a proxy to treat as two different values.
        BookCoverImage.Create(BookId, "IMAGE/PNG", Png(), Now).Value
            .ContentType.Should().Be("image/png");
    }
}
