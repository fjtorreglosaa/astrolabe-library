using Astrolabe.Domain.Features.Catalog.Errors;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Domain.Features.Catalog.Policies;

/// <summary>
/// What may be accepted as a cover image. Backs BR-CAT-005's other half — a book either has a real
/// cover or it has a tint, and a broken image is neither.
///
/// <para>
/// The limits are the prototype's: images only, four megabytes. The <b>magic byte</b> check is not:
/// a client can claim any content type it likes, and storing a file that says it is a PNG and is not
/// means later serving something unexpected to a browser under an image content type. The declared
/// type is a hint; the first bytes are evidence.
/// </para>
/// </summary>
public static class CoverImagePolicy
{
    /// <summary>The prototype's own limit, and generous for a book cover.</summary>
    public const int MaxBytes = 4 * 1024 * 1024;

    public static readonly IReadOnlySet<string> AllowedContentTypes =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/png",
            "image/webp",
        };

    public static Result EnsureAcceptable(string? contentType, byte[]? content)
    {
        if (content is null || content.Length == 0)
        {
            return Result.Failure(CatalogErrors.CoverImageEmpty);
        }

        if (content.Length > MaxBytes)
        {
            return Result.Failure(CatalogErrors.CoverImageTooLarge);
        }

        if (contentType is null || !AllowedContentTypes.Contains(contentType))
        {
            return Result.Failure(CatalogErrors.CoverImageTypeNotAllowed);
        }

        // The declared type and the bytes must agree. A mismatch is not a mistake worth guessing
        // about — it is either a broken upload or somebody trying something.
        if (!Matches(contentType, content))
        {
            return Result.Failure(CatalogErrors.CoverImageNotAnImage);
        }

        return Result.Success();
    }

    /// <summary>
    /// Reads the file signature. Deliberately narrow: three formats, three signatures, and anything
    /// else is refused rather than sniffed further.
    /// </summary>
    private static bool Matches(string contentType, byte[] content) =>
        contentType.ToLowerInvariant() switch
        {
            // FF D8 FF — every JPEG variant starts with it.
            "image/jpeg" => content.Length >= 3
                && content[0] == 0xFF && content[1] == 0xD8 && content[2] == 0xFF,

            // 89 P N G \r \n 1A \n
            "image/png" => content.Length >= 8
                && content[0] == 0x89 && content[1] == 0x50 && content[2] == 0x4E
                && content[3] == 0x47 && content[4] == 0x0D && content[5] == 0x0A
                && content[6] == 0x1A && content[7] == 0x0A,

            // RIFF....WEBP
            "image/webp" => content.Length >= 12
                && content[0] == 0x52 && content[1] == 0x49 && content[2] == 0x46 && content[3] == 0x46
                && content[8] == 0x57 && content[9] == 0x45 && content[10] == 0x42 && content[11] == 0x50,

            _ => false
        };
}
