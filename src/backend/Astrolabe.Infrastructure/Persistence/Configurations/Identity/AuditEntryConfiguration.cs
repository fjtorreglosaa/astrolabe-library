using Astrolabe.Domain.Features.Identity.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Astrolabe.Infrastructure.Persistence.Configurations.Identity;

public sealed class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.ToTable("audit_entries");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Action).HasMaxLength(100).IsRequired();
        builder.Property(a => a.ActorUserId);
        builder.Property(a => a.SubjectUserId);
        builder.Property(a => a.IpAddress).HasMaxLength(45);
        builder.Property(a => a.Detail).HasMaxLength(1000);
        builder.Property(a => a.OccurredAt).IsRequired();

        // No foreign key to users on purpose: an audit trail must outlive the account it describes,
        // and a cascade delete would erase exactly the record of the deletion.
        builder.HasIndex(a => new { a.SubjectUserId, a.OccurredAt });
        builder.HasIndex(a => a.Action);
    }
}
