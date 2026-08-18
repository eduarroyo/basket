using BasketBaseTracker.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BasketBaseTracker.Web.Data.Configurations;

public class PartidoParcialConfiguration : IEntityTypeConfiguration<PartidoParcial>
{
    public void Configure(EntityTypeBuilder<PartidoParcial> builder)
    {
        builder.HasIndex(p => new { p.PartidoId, p.NumeroPeriodo }).IsUnique();

        builder.HasOne(p => p.Partido)
            .WithMany()
            .HasForeignKey(p => p.PartidoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
