using Astrolabe.Domain.Features.Identity.Entities;
using Astrolabe.Domain.Features.Identity.ValueObjects;
using Astrolabe.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Astrolabe.Infrastructure.Persistence.Configurations.Identity;

public sealed class SingleUseTokenConfiguration : IEntityTypeConfiguration<SingleUseToken>
{
    public void Configure(EntityTypeBuilder<SingleUseToken> builder)
    {
        builder.ToTable("single_use_tokens");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.UserId).IsRequired();
        builder.Property(t => t.Purpose).HasConversion<int>().IsRequired();

        builder.Property(t => t.Hash)
            .HasConversion<SecretHashConverter>()
            .HasColumnType("bytea")
            .HasMaxLength(SecretHash.ByteLength)
            .IsRequired();

        builder.Property(t => t.IssuedAt).IsRequired();
        builder.Property(t => t.ExpiresAt).IsRequired();
        builder.Property(t => t.ConsumedAt);
        builder.Property(t => t.InvalidatedAt);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => t.Hash).IsUnique();

        // Backs BR-IDN-005: issuing a new token retires the outstanding ones for that purpose.
        builder.HasIndex(t => new { t.UserId, t.Purpose, t.ConsumedAt, t.InvalidatedAt });
    }
}
