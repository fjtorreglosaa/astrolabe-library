using Astrolabe.Domain.Features.Network.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Astrolabe.Infrastructure.Persistence.Configurations.Network;

public sealed class AdminInvitationConfiguration : IEntityTypeConfiguration<AdminInvitation>
{
    public void Configure(EntityTypeBuilder<AdminInvitation> builder)
    {
        builder.ToTable("admin_invitations");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.UserId).IsRequired();
        builder.Property(i => i.Role).HasConversion<int>().IsRequired();
        builder.Property(i => i.InvitedByUserId).IsRequired();
        builder.Property(i => i.CreatedAt).IsRequired();
        builder.Property(i => i.ExpiresAt).IsRequired();
        builder.Property(i => i.AcceptedAt);
        builder.Property(i => i.RevokedAt);

        // SHA-256 of the emailed token. No plaintext column exists, so BR-IDN-016's sibling rule
        // for invitations cannot be violated by a future careless write.
        builder.Property(i => i.TokenHash).HasColumnType("bytea").IsRequired();

        // The libraries an invitation grants are a snapshot, not a relationship: they must not
        // change if the library set is later edited. Stored as an array column.
        builder.PrimitiveCollection(i => i.LibraryIds).IsRequired();

        builder.Ignore(i => i.IsPending);
        builder.Ignore(i => i.DomainEvents);

        builder.HasIndex(i => i.TokenHash);
        builder.HasIndex(i => i.UserId);
    }
}
