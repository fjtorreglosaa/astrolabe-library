using Astrolabe.Domain.Features.Recommendations.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Astrolabe.Infrastructure.Persistence.Configurations.Recommendations;

public sealed class LibraryAiConfigurationConfiguration
    : IEntityTypeConfiguration<LibraryAiConfiguration>
{
    public void Configure(EntityTypeBuilder<LibraryAiConfiguration> builder)
    {
        builder.ToTable("library_ai_configurations");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.LibraryId).IsRequired();
        builder.Property(c => c.Provider).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(c => c.IsVerified).IsRequired();
        builder.Property(c => c.IsEnabled).IsRequired();
        builder.Property(c => c.CreatedAt).IsRequired();
        builder.Property(c => c.LastVerifiedAt);
        builder.Property(c => c.LastFailureAt);

        // An owned type rather than a value converter, per RULE 23 — but note that this one is never
        // filtered or ordered on, so the usual reason does not apply. It is owned because a
        // converter would need a way to read the plaintext bytes back out of the value object, and
        // EncryptedSecret deliberately has none.
        builder.OwnsOne(c => c.Credential, credential =>
        {
            credential.Property(s => s.CipherText).HasColumnName("credential_cipher").IsRequired();
            credential.Property(s => s.KeyVersion)
                .HasColumnName("credential_key_version").HasMaxLength(128).IsRequired();
        });
        builder.Navigation(c => c.Credential).IsRequired();

        builder.Ignore(c => c.IsConnected);
        builder.Ignore(c => c.DomainEvents);

        // One configuration per library. BR-REC-001 gives each library its own, and exactly one.
        builder.HasIndex(c => c.LibraryId).IsUnique();

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();
    }
}
