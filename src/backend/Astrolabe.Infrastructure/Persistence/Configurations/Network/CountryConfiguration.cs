using Astrolabe.Domain.Features.Network.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Astrolabe.Infrastructure.Persistence.Configurations.Network;

public sealed class CountryConfiguration : IEntityTypeConfiguration<Country>
{
    public void Configure(EntityTypeBuilder<Country> builder)
    {
        builder.ToTable("countries");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).HasMaxLength(120).IsRequired();

        builder.Property(c => c.IsoCode).HasMaxLength(2).IsFixedLength().IsRequired();

        builder.Property(c => c.IsHiddenFromRegistration).IsRequired();

        builder.HasIndex(c => c.IsoCode).IsUnique();
    }
}
