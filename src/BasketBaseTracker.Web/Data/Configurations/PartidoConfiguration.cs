using BasketBaseTracker.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BasketBaseTracker.Web.Data.Configurations;

public class PartidoConfiguration : IEntityTypeConfiguration<Partido>
{
    public void Configure(EntityTypeBuilder<Partido> builder)
    {
        builder.Property(p => p.Estado).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.MotivoResolucion).HasConversion<string>().HasMaxLength(30);
        builder.Property(p => p.Observaciones).HasMaxLength(500);
        builder.Property(p => p.RowVersion).IsRowVersion();

        builder.HasOne(p => p.Jornada)
            .WithMany()
            .HasForeignKey(p => p.JornadaId)
            .OnDelete(DeleteBehavior.Restrict);

        // Tres claves foráneas distintas a Equipo: las tres en Restrict, o SQL
        // Server rechaza la migración por "multiple cascade paths" (plan.md).
        builder.HasOne(p => p.EquipoLocal)
            .WithMany()
            .HasForeignKey(p => p.EquipoLocalId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.EquipoVisitante)
            .WithMany()
            .HasForeignKey(p => p.EquipoVisitanteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.EquipoGanadorResolucion)
            .WithMany()
            .HasForeignKey(p => p.EquipoGanadorResolucionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Sede)
            .WithMany()
            .HasForeignKey(p => p.SedeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
