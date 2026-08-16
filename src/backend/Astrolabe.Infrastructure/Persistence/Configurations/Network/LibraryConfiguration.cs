using Astrolabe.Domain.Features.Network.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Astrolabe.Infrastructure.Persistence.Configurations.Network;

public sealed class LibraryConfiguration : IEntityTypeConfiguration<Library>
{
    public void Configure(EntityTypeBuilder<Library> builder)
    {
        builder.ToTable("libraries");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Name).HasMaxLength(120).IsRequired();

        builder.Property(l => l.CityId).IsRequired();

        builder.Property(l => l.IsActive).IsRequired();

        builder.HasOne<City>()
            .WithMany()
            .HasForeignKey(l => l.CityId)
            .OnDelete(DeleteBehavior.Restrict);

        // BR-NET-002 enforced by the database. An application check alone races between
        // check and insert under concurrency.
        builder.HasIndex(l => new { l.CityId, l.Name }).IsUnique();

        builder.HasIndex(l => l.IsActive);
    }
}
