using BasketBaseTracker.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BasketBaseTracker.Web.Data.Configurations;

public class PenalizacionClasificacionConfiguration : IEntityTypeConfiguration<PenalizacionClasificacion>
{
    public void Configure(EntityTypeBuilder<PenalizacionClasificacion> builder)
    {
        builder.Property(p => p.Motivo).HasMaxLength(500).IsRequired();

        builder.HasOne(p => p.Equipo)
            .WithMany()
            .HasForeignKey(p => p.EquipoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Partido)
            .WithMany()
            .HasForeignKey(p => p.PartidoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
