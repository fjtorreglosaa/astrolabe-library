using Astrolabe.Domain.Features.Billing.Entities;
using Astrolabe.Domain.Features.Identity.Entities;
using Astrolabe.Domain.Features.Network.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Astrolabe.Infrastructure.Persistence.Configurations.Billing;

public sealed class DeskPaymentConfiguration : IEntityTypeConfiguration<DeskPayment>
{
    public void Configure(EntityTypeBuilder<DeskPayment> builder)
    {
        builder.ToTable("desk_payments");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.MemberId).IsRequired();
        builder.Property(d => d.LibraryId).IsRequired();
        builder.Property(d => d.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(d => d.IssuedAt).IsRequired();
        builder.Property(d => d.ExpiresAt).IsRequired();
        builder.Property(d => d.ResolvedAt);
        builder.Property(d => d.RejectionReason).HasMaxLength(500);

        // Owned rather than converted: the code is looked up by value at a counter.
        builder.OwnsOne(d => d.Code, code =>
        {
            code.Property(c => c.Value).HasColumnName("code").HasMaxLength(16).IsRequired();
            code.HasIndex(c => c.Value).IsUnique();
        });
        builder.Navigation(d => d.Code).IsRequired();

        builder.ComplexProperty(d => d.Amount, amount =>
            amount.Property(m => m.Cents).HasColumnName("amount_cents").IsRequired());

        // The fines a code covers. A primitive collection rather than a join table: the list is
        // short, immutable and never queried from the other direction — the fine already carries
        // its DeskPaymentId for that.
        builder.PrimitiveCollection(d => d.FineIds).HasColumnName("fine_ids").IsRequired();

        builder.Ignore(d => d.DomainEvents);

        builder.HasOne<User>().WithMany().HasForeignKey(d => d.MemberId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Library>().WithMany().HasForeignKey(d => d.LibraryId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(d => new { d.LibraryId, d.Status });
        builder.HasIndex(d => new { d.MemberId, d.Status });
    }
}
