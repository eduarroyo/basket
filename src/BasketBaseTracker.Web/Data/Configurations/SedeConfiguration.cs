using BasketBaseTracker.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BasketBaseTracker.Web.Data.Configurations;

public class SedeConfiguration : IEntityTypeConfiguration<Sede>
{
    public void Configure(EntityTypeBuilder<Sede> builder)
    {
        builder.Property(s => s.Nombre).HasMaxLength(200).IsRequired();
        builder.Property(s => s.Municipio).HasMaxLength(200).IsRequired();
        builder.Property(s => s.Direccion).HasMaxLength(500).IsRequired();
    }
}
