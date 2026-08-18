using BasketBaseTracker.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BasketBaseTracker.Web.Data.Configurations;

public class FichaJugadorConfiguration : IEntityTypeConfiguration<FichaJugador>
{
    public void Configure(EntityTypeBuilder<FichaJugador> builder)
    {
        builder.HasIndex(f => new { f.EquipoId, f.Dorsal }).IsUnique();

        builder.Property(f => f.Posicion).HasConversion<string>().HasMaxLength(20);

        builder.HasOne(f => f.Equipo)
            .WithMany()
            .HasForeignKey(f => f.EquipoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
