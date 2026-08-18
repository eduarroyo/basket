using BasketBaseTracker.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BasketBaseTracker.Web.Data.Configurations;

public class EquipoConfiguration : IEntityTypeConfiguration<Equipo>
{
    public void Configure(EntityTypeBuilder<Equipo> builder)
    {
        builder.Property(e => e.Nombre).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Estado).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.RowVersion).IsRowVersion();

        builder.HasOne(e => e.Competicion)
            .WithMany()
            .HasForeignKey(e => e.CompeticionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Club)
            .WithMany()
            .HasForeignKey(e => e.ClubId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.SedeHabitual)
            .WithMany()
            .HasForeignKey(e => e.SedeHabitualId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
