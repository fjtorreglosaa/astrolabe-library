using Astrolabe.Domain.Features.Identity.Entities;
using Astrolabe.Domain.Features.Store.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Astrolabe.Infrastructure.Persistence.Configurations.Store;

public sealed class PointsMovementConfiguration : IEntityTypeConfiguration<PointsMovement>
{
    public void Configure(EntityTypeBuilder<PointsMovement> builder)
    {
        builder.ToTable("points_movements");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.MemberId).IsRequired();
        builder.Property(m => m.PointCents).IsRequired();
        builder.Property(m => m.Description).HasMaxLength(300).IsRequired();
        builder.Property(m => m.OrderId);
        builder.Property(m => m.OccurredAt).IsRequired();

        builder.HasOne<User>().WithMany().HasForeignKey(m => m.MemberId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(m => new { m.MemberId, m.OccurredAt });
    }
}
