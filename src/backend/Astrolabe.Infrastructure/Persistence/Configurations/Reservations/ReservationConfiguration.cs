using Astrolabe.Domain.Features.Catalog.Entities;
using Astrolabe.Domain.Features.Identity.Entities;
using Astrolabe.Domain.Features.Network.Entities;
using Astrolabe.Domain.Features.Reservations.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Astrolabe.Infrastructure.Persistence.Configurations.Reservations;

public sealed class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.ToTable("reservations");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.MemberId).IsRequired();
        builder.Property(r => r.BookId).IsRequired();
        builder.Property(r => r.BookCopyId).IsRequired();
        builder.Property(r => r.LibraryId).IsRequired();

        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(r => r.Delivery).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(r => r.ReturnMethod).HasConversion<string>().HasMaxLength(16);

        builder.Property(r => r.ConfirmedAt).IsRequired();
        builder.Property(r => r.HandedOverAt);
        builder.Property(r => r.CheckedInAt);
        builder.Property(r => r.DaysLateAtCheckIn).IsRequired();
        builder.Property(r => r.IdempotencyKey).HasMaxLength(100);

        // Owned, not converted. Every listing in the product orders by the due date, and a value
        // converter would make `Period.DueOn` untranslatable — see GUIDELINES.md section 14.1.
        builder.OwnsOne(r => r.Period, period =>
        {
            period.Property(p => p.StartedOn).HasColumnName("borrowed_on").IsRequired();
            period.Property(p => p.DueOn).HasColumnName("due_on").IsRequired();
            period.HasIndex(p => p.DueOn);
        });
        builder.Navigation(r => r.Period).IsRequired();

        // A complex type for the same reason: money is filtered and summed on.
        builder.ComplexProperty(r => r.DeliveryFee, fee =>
            fee.Property(m => m.Cents).HasColumnName("delivery_fee_cents").IsRequired());

        builder.Ignore(r => r.IsActive);
        builder.Ignore(r => r.HandoverCode);
        builder.Ignore(r => r.RestoresStock);
        builder.Ignore(r => r.DomainEvents);

        // Restricted, never cascaded: a record of who holds the library's property must outlive the
        // account, and deleting a book must not erase the loans that proved it existed.
        builder.HasOne<User>().WithMany().HasForeignKey(r => r.MemberId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Book>().WithMany().HasForeignKey(r => r.BookId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<BookCopy>().WithMany().HasForeignKey(r => r.BookCopyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Library>().WithMany().HasForeignKey(r => r.LibraryId).OnDelete(DeleteBehavior.Restrict);

        // BR-RSV-008 in the database. Filtered, so the many rows without a key do not collide.
        builder.HasIndex(r => new { r.MemberId, r.IdempotencyKey })
            .IsUnique()
            .HasFilter("idempotency_key IS NOT NULL");

        // The two listings the product actually has.
        builder.HasIndex(r => new { r.MemberId, r.Status });
        builder.HasIndex(r => new { r.LibraryId, r.Status });
    }
}
