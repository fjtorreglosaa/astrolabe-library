using Astrolabe.Domain.Features.Catalog.Entities;
using Astrolabe.Domain.Features.Store.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Astrolabe.Infrastructure.Persistence.Configurations.Store;

public sealed class OrderLineConfiguration : IEntityTypeConfiguration<OrderLine>
{
    public void Configure(EntityTypeBuilder<OrderLine> builder)
    {
        builder.ToTable("order_lines");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.OrderId).IsRequired();
        builder.Property(l => l.BookId).IsRequired();
        builder.Property(l => l.BookTitle).HasMaxLength(300).IsRequired();
        builder.Property(l => l.Quantity).IsRequired();
        builder.Property(l => l.DiscountPercent).IsRequired();

        builder.ComplexProperty(l => l.UnitPrice, m =>
            m.Property(x => x.Cents).HasColumnName("unit_price_cents").IsRequired());
        builder.ComplexProperty(l => l.DiscountAmount, m =>
            m.Property(x => x.Cents).HasColumnName("discount_amount_cents").IsRequired());
        builder.ComplexProperty(l => l.LineTotal, m =>
            m.Property(x => x.Cents).HasColumnName("line_total_cents").IsRequired());

        builder.Ignore(l => l.GrossTotal);

        // Restricted, never cascaded: removing a book must not erase the receipts that sold it.
        builder.HasOne<Book>().WithMany().HasForeignKey(l => l.BookId).OnDelete(DeleteBehavior.Restrict);
    }
}
