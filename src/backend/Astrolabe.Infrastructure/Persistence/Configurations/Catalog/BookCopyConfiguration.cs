using Astrolabe.Domain.Features.Catalog.Entities;
using Astrolabe.Domain.Features.Network.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Astrolabe.Infrastructure.Persistence.Configurations.Catalog;

public sealed class BookCopyConfiguration : IEntityTypeConfiguration<BookCopy>
{
    public void Configure(EntityTypeBuilder<BookCopy> builder)
    {
        builder.ToTable("book_copies");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.BookId).IsRequired();
        builder.Property(c => c.LibraryId).IsRequired();
        builder.Property(c => c.TotalCount).IsRequired();
        builder.Property(c => c.AvailableCount).IsRequired();

        builder.Ignore(c => c.HasStock);

        builder.HasOne<Library>()
            .WithMany()
            .HasForeignKey(c => c.LibraryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Two members taking the last copy must not both succeed. The in-memory guard keeps the
        // count sane; this is what makes the race safe at commit.
        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        // One holding per book and library, which is what makes the merge in AddCopies the only way
        // stock grows.
        builder.HasIndex(c => new { c.BookId, c.LibraryId }).IsUnique();
    }
}
