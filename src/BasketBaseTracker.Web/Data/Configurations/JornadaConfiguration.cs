using BasketBaseTracker.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BasketBaseTracker.Web.Data.Configurations;

public class JornadaConfiguration : IEntityTypeConfiguration<Jornada>
{
    public void Configure(EntityTypeBuilder<Jornada> builder)
    {
        builder.HasIndex(j => new { j.CompeticionId, j.Numero }).IsUnique();

        builder.Property(j => j.Etiqueta).HasMaxLength(200);
        builder.Property(j => j.CuentaParaClasificacion).HasDefaultValue(true);

        builder.HasOne(j => j.Competicion)
            .WithMany()
            .HasForeignKey(j => j.CompeticionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
