using Astrolabe.Domain.Features.Identity.Entities;
using Astrolabe.Domain.Features.Identity.ValueObjects;
using Astrolabe.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Astrolabe.Infrastructure.Persistence.Configurations.Identity;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.SessionId).IsRequired();

        // BR-IDN-016: only the hash is stored, as bytea. There is deliberately no column that
        // could hold a plaintext token.
        builder.Property(t => t.Hash)
            .HasConversion<SecretHashConverter>()
            .HasColumnType("bytea")
            .HasMaxLength(SecretHash.ByteLength)
            .IsRequired();

        builder.Property(t => t.IssuedAt).IsRequired();
        builder.Property(t => t.ExpiresAt).IsRequired();
        builder.Property(t => t.RotatedAt);
        builder.Property(t => t.ReplacedByTokenId);

        builder.Ignore(t => t.IsRotated);

        // The lookup that starts every refresh. Unique because a hash collision would mean two
        // sessions could be reached with one token.
        builder.HasIndex(t => t.Hash).IsUnique();
    }
}
