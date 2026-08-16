using Astrolabe.Domain.Features.Billing.Entities;
using Astrolabe.Domain.Features.Identity.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Astrolabe.Infrastructure.Persistence.Configurations.Billing;

public sealed class LedgerEntryConfiguration : IEntityTypeConfiguration<LedgerEntry>
{
    public void Configure(EntityTypeBuilder<LedgerEntry> builder)
    {
        builder.ToTable("ledger_entries");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.MemberId).IsRequired();
        builder.Property(e => e.Kind).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(300).IsRequired();
        builder.Property(e => e.FineId);
        builder.Property(e => e.ReservationId);
        builder.Property(e => e.OccurredAt).IsRequired();

        // Summed in SQL for the balance, so the member must stay queryable.
        builder.ComplexProperty(e => e.Amount, amount =>
            amount.Property(m => m.Cents).HasColumnName("amount_cents").IsRequired());

        // Restricted, never cascaded: the ledger outlives the account it describes.
        builder.HasOne<User>().WithMany().HasForeignKey(e => e.MemberId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.MemberId, e.OccurredAt });
    }
}
