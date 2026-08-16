using Astrolabe.Domain.Features.Catalog.Entities;
using Astrolabe.Domain.Features.Identity.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Astrolabe.Infrastructure.Persistence.Configurations.Catalog;

public sealed class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("reviews");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.BookId).IsRequired();
        builder.Property(r => r.MemberId).IsRequired();
        builder.Property(r => r.Comment).HasMaxLength(2000);
        builder.Property(r => r.CreatedAt).IsRequired();
        builder.Property(r => r.EditedAt);

        // Owned for the same reason as the ISBN: the rating is averaged in the database, and a
        // value converter would leave `review.Rating.Stars` untranslatable.
        builder.OwnsOne(r => r.Rating, rating =>
            rating.Property(s => s.Stars).HasColumnName("rating").IsRequired());
        builder.Navigation(r => r.Rating).IsRequired();

        builder.Ignore(r => r.DomainEvents);

        builder.HasOne<Book>()
            .WithMany()
            .HasForeignKey(r => r.BookId)
            .OnDelete(DeleteBehavior.Cascade);

        // A deleted member's reviews stay visible and keep counting toward the rating, so the
        // author is restricted rather than cascaded.
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(r => r.MemberId)
            .OnDelete(DeleteBehavior.Restrict);

        // BR-CAT-027, at most one review per member and book. The handler edits rather than
        // inserting; this is what holds if two requests race.
        builder.HasIndex(r => new { r.BookId, r.MemberId }).IsUnique();
    }
}
