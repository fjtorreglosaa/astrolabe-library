using Astrolabe.Domain.Features.Billing.Entities;
using Astrolabe.Domain.Features.Identity.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Astrolabe.Infrastructure.Persistence.Configurations.Billing;

public sealed class PaymentMethodConfiguration : IEntityTypeConfiguration<PaymentMethod>
{
    public void Configure(EntityTypeBuilder<PaymentMethod> builder)
    {
        builder.ToTable("payment_methods");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.MemberId).IsRequired();
        builder.Property(p => p.Brand).HasConversion<string>().HasMaxLength(16).IsRequired();

        // Four characters, not "up to four". The column itself refuses to hold a card number, so
        // even a direct database write cannot put one here. BR-BIL-006 twice over.
        builder.Property(p => p.Last4).HasMaxLength(4).IsFixedLength().IsRequired();

        builder.Property(p => p.ExpiryMonthYear).HasMaxLength(5).IsRequired();
        builder.Property(p => p.CardholderName).HasMaxLength(200).IsRequired();
        builder.Property(p => p.IsPrimary).IsRequired();

        builder.Ignore(p => p.DisplayName);

        builder.HasOne<User>().WithMany().HasForeignKey(p => p.MemberId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => p.MemberId);
    }
}
