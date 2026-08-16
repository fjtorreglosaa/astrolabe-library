using Astrolabe.Domain.Features.Identity.Entities;
using Astrolabe.Domain.Features.Store.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Astrolabe.Infrastructure.Persistence.Configurations.Store;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.MemberId).IsRequired();
        builder.Property(o => o.Fulfilment).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(o => o.PointsEarned).IsRequired();
        builder.Property(o => o.PlacedAt).IsRequired();
        builder.Property(o => o.IdempotencyKey).HasMaxLength(100);

        // Complex types, never converters: order totals are summed and filtered on.
        builder.ComplexProperty(o => o.Subtotal, m =>
            m.Property(x => x.Cents).HasColumnName("subtotal_cents").IsRequired());
        builder.ComplexProperty(o => o.DiscountTotal, m =>
            m.Property(x => x.Cents).HasColumnName("discount_total_cents").IsRequired());
        builder.ComplexProperty(o => o.ShippingFee, m =>
            m.Property(x => x.Cents).HasColumnName("shipping_fee_cents").IsRequired());
        builder.ComplexProperty(o => o.Total, m =>
            m.Property(x => x.Cents).HasColumnName("total_cents").IsRequired());

        builder.Ignore(o => o.Description);
        builder.Ignore(o => o.DomainEvents);

        builder.HasMany(o => o.Lines)
            .WithOne()
            .HasForeignKey(l => l.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // The aggregate mutates the backing list, so EF must write through the field.
        builder.Navigation(o => o.Lines).HasField("_lines");

        // Restricted: a purchase record outlives the account that made it.
        builder.HasOne<User>().WithMany().HasForeignKey(o => o.MemberId).OnDelete(DeleteBehavior.Restrict);

        // BR-STR-015 in the database. Filtered, so the many rows without a key do not collide.
        builder.HasIndex(o => new { o.MemberId, o.IdempotencyKey })
            .IsUnique()
            .HasFilter("idempotency_key IS NOT NULL");

        builder.HasIndex(o => new { o.MemberId, o.PlacedAt });
    }
}
