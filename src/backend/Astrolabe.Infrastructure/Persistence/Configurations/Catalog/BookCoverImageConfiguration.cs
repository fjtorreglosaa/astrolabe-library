using Astrolabe.Domain.Features.Catalog.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Astrolabe.Infrastructure.Persistence.Configurations.Catalog;

public sealed class BookCoverImageConfiguration : IEntityTypeConfiguration<BookCoverImage>
{
    public void Configure(EntityTypeBuilder<BookCoverImage> builder)
    {
        builder.ToTable("book_covers");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.BookId).IsRequired();
        builder.Property(c => c.ContentType).HasMaxLength(64).IsRequired();
        builder.Property(c => c.Content).IsRequired();
        builder.Property(c => c.UploadedAt).IsRequired();

        // One cover per book. A second row would leave two answers to "what does this look like".
        builder.HasIndex(c => c.BookId).IsUnique();
    }
}
