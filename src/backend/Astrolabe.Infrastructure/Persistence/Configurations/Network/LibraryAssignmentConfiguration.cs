using Astrolabe.Domain.Features.Network.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Astrolabe.Infrastructure.Persistence.Configurations.Network;

public sealed class LibraryAssignmentConfiguration : IEntityTypeConfiguration<LibraryAssignment>
{
    public void Configure(EntityTypeBuilder<LibraryAssignment> builder)
    {
        builder.ToTable("library_assignments");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.UserId).IsRequired();
        builder.Property(a => a.LibraryId).IsRequired();
        builder.Property(a => a.GrantedByUserId).IsRequired();
        builder.Property(a => a.GrantedAt).IsRequired();
        builder.Property(a => a.RevokedAt);
        builder.Property(a => a.RevokedByUserId);

        builder.Ignore(a => a.IsActive);
        builder.Ignore(a => a.DomainEvents);

        builder.HasOne<Library>()
            .WithMany()
            .HasForeignKey(a => a.LibraryId)
            .OnDelete(DeleteBehavior.Restrict);

        // One active assignment per user and library. Revoked rows are excluded so the same
        // library can be granted again after a revocation.
        builder.HasIndex(a => new { a.UserId, a.LibraryId })
            .IsUnique()
            .HasFilter("revoked_at IS NULL");

        // Scope resolution runs once per staff request and always filters on these two columns.
        builder.HasIndex(a => new { a.UserId, a.RevokedAt });
    }
}
