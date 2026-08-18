using BasketBaseTracker.Web.Data.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BasketBaseTracker.Web.Data;

// Los reintentos ante fallos transitorios de Azure SQL (EnableRetryOnFailure) se
// configuran en Program.cs, no aquí vía OnConfiguring: AddSqlServerDbContext usa
// un pool de contextos, y EF Core prohíbe sobreescribir OnConfiguring cuando el
// pooling está activo — ver spec.md de BAS-3.
//
// Mismo DbContext que Identity (no uno separado para el dominio): una sola base
// de datos, una sola cadena de migraciones — BAS-5, spec.md, "Aclaraciones".
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext(options)
{
    public DbSet<Categoria> Categorias => Set<Categoria>();

    public DbSet<Club> Clubes => Set<Club>();

    public DbSet<Sede> Sedes => Set<Sede>();

    public DbSet<Temporada> Temporadas => Set<Temporada>();

    public DbSet<Competicion> Competiciones => Set<Competicion>();

    public DbSet<Equipo> Equipos => Set<Equipo>();

    public DbSet<FichaJugador> FichasJugador => Set<FichaJugador>();

    public DbSet<Jornada> Jornadas => Set<Jornada>();

    public DbSet<Partido> Partidos => Set<Partido>();

    public DbSet<PartidoParcial> PartidosParciales => Set<PartidoParcial>();

    public DbSet<PenalizacionClasificacion> PenalizacionesClasificacion => Set<PenalizacionClasificacion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
