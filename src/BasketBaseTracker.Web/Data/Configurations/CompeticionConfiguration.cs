using BasketBaseTracker.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BasketBaseTracker.Web.Data.Configurations;

public class CompeticionConfiguration : IEntityTypeConfiguration<Competicion>
{
    public void Configure(EntityTypeBuilder<Competicion> builder)
    {
        // Una competición por categoría y temporada (data-model.md).
        builder.HasIndex(c => new { c.TemporadaId, c.CategoriaId }).IsUnique();

        builder.Property(c => c.PuntosVictoria).HasDefaultValue(2);
        builder.Property(c => c.PuntosDerrota).HasDefaultValue(1);

        builder.HasOne(c => c.Temporada)
            .WithMany()
            .HasForeignKey(c => c.TemporadaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Categoria)
            .WithMany()
            .HasForeignKey(c => c.CategoriaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
