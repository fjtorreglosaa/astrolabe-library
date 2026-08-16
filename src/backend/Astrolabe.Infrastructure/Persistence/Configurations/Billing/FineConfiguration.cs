using Astrolabe.Domain.Features.Billing.Entities;
using Astrolabe.Domain.Features.Identity.Entities;
using Astrolabe.Domain.Features.Network.Entities;
using Astrolabe.Domain.Features.Reservations.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Astrolabe.Infrastructure.Persistence.Configurations.Billing;

public sealed class FineConfiguration : IEntityTypeConfiguration<Fine>
{
    public void Configure(EntityTypeBuilder<Fine> builder)
    {
        builder.ToTable("fines");
        builder.HasKey(f => f.Id);

        builder.Property(f => f.MemberId).IsRequired();
        builder.Property(f => f.ReservationId).IsRequired();
        builder.Property(f => f.LibraryId).IsRequired();
        builder.Property(f => f.BookTitle).HasMaxLength(300).IsRequired();
        builder.Property(f => f.DaysLate).IsRequired();
        builder.Property(f => f.Status).HasConversion<string>().HasMaxLength(24).IsRequired();
        builder.Property(f => f.AssessedAt).IsRequired();
        builder.Property(f => f.SettledAt);
        builder.Property(f => f.DeskPaymentId);

        // A complex type, never a converter: fines are summed for the outstanding total on every
        // screen that shows money owed. See GUIDELINES.md section 14.1.
        builder.ComplexProperty(f => f.Amount, amount =>
            amount.Property(m => m.Cents).HasColumnName("amount_cents").IsRequired());

        builder.Ignore(f => f.IsOutstanding);
        builder.Ignore(f => f.IsOwed);
        builder.Ignore(f => f.DomainEvents);

        builder.HasOne<User>().WithMany().HasForeignKey(f => f.MemberId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Reservation>().WithMany().HasForeignKey(f => f.ReservationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Library>().WithMany().HasForeignKey(f => f.LibraryId).OnDelete(DeleteBehavior.Restrict);

        // BR-BIL-010 in the database. One reservation, one fine, however many times it is assessed.
        builder.HasIndex(f => f.ReservationId).IsUnique();
        builder.HasIndex(f => new { f.MemberId, f.Status });
        builder.HasIndex(f => f.DeskPaymentId);
    }
}
