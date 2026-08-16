using Astrolabe.Domain.Features.Recommendations.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Astrolabe.Infrastructure.Persistence.Configurations.Recommendations;

public sealed class RecommendationSetConfiguration : IEntityTypeConfiguration<RecommendationSet>
{
    public void Configure(EntityTypeBuilder<RecommendationSet> builder)
    {
        builder.ToTable("recommendation_sets");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.MemberId).IsRequired();
        builder.Property(s => s.Source).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(s => s.GeneratedByLibraryId);
        builder.Property(s => s.GeneratedAt).IsRequired();
        builder.Property(s => s.ExpiresAt).IsRequired();

        builder.Ignore(s => s.DomainEvents);

        // The items are meaningless without their set and are never queried alone, so they stay
        // owned rather than becoming a table nobody joins to independently.
        builder.OwnsMany(s => s.Items, item =>
        {
            item.ToTable("recommendation_items");
            item.WithOwner().HasForeignKey("recommendation_set_id");
            item.HasKey(i => i.Id);
            item.Property(i => i.BookId).IsRequired();
            item.Property(i => i.Reason).HasMaxLength(RecommendationItem.MaxReasonLength).IsRequired();
            item.Property(i => i.MatchPercent).IsRequired();
        });

        // One live set per member, which is what the generator maintains by replacing rather than
        // accumulating. Indexed so the read path is a single seek.
        builder.HasIndex(s => s.MemberId).IsUnique();

        // BR-REC-012 evicts by library, so that lookup gets its own index.
        builder.HasIndex(s => s.GeneratedByLibraryId);
    }
}
