using Astrolabe.Domain.Features.Catalog.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Astrolabe.Infrastructure.Persistence.Configurations.Catalog;

public sealed class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.ToTable("books");

        builder.HasKey(b => b.Id);

        // Owned rather than converted. A value converter turns the whole value object into an
        // opaque scalar, so `book.Isbn.Value` becomes untranslatable and every search on the ISBN
        // fails at run time. Owning it keeps the same single column and keeps the member queryable.
        builder.OwnsOne(b => b.Isbn, isbn =>
        {
            isbn.Property(i => i.Value).HasColumnName("isbn").HasMaxLength(13).IsRequired();

            // BR-CAT-003 under concurrency. The check in the handler gives a clean message; this is
            // what actually holds when two staff members submit the same ISBN at once.
            isbn.HasIndex(i => i.Value).IsUnique();
        });
        builder.Navigation(b => b.Isbn).IsRequired();

        builder.Property(b => b.Title).HasMaxLength(300).IsRequired();
        builder.Property(b => b.Author).HasMaxLength(200).IsRequired();
        builder.Property(b => b.Publisher).HasMaxLength(200);
        builder.Property(b => b.Genre).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(b => b.Tier).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(b => b.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(b => b.CoverUrl).HasMaxLength(1000);
        builder.Property(b => b.CreatedAt).IsRequired();

        // Money is integer cents everywhere: storing a decimal would reintroduce the rounding the
        // primitive exists to prevent.
        //
        // Mapped as a complex type rather than with a value converter, for the same reason as the
        // ISBN: a converter hides Cents from the provider, and the catalogue sorts on price. It was
        // a converter first, and ordering by price returned 500 in the running system.
        builder.ComplexProperty(b => b.RetailPrice, price =>
            price.Property(m => m.Cents).HasColumnName("retail_price_cents").IsRequired());

        // Precision is fixed because the value is a mean of integers from 1 to 5; anything wider
        // would store noise that no screen renders.
        builder.Property(b => b.AverageRating).HasPrecision(3, 2);
        builder.Property(b => b.ReviewCount).IsRequired();

        builder.Ignore(b => b.IsVisibleToMembers);
        builder.Ignore(b => b.DomainEvents);

        builder.HasMany(b => b.Copies)
            .WithOne()
            .HasForeignKey(c => c.BookId)
            .OnDelete(DeleteBehavior.Cascade);

        // The aggregate exposes a read-only view and mutates the backing list itself, so EF must
        // write through the field rather than through the property.
        builder.Navigation(b => b.Copies).HasField("_copies");

        // Every member-facing listing filters on the status first, then usually on the genre.
        builder.HasIndex(b => new { b.Status, b.Genre });
    }
}
