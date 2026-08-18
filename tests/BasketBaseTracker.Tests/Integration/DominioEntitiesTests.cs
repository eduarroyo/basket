using BasketBaseTracker.Web.Data;
using BasketBaseTracker.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BasketBaseTracker.Tests.Integration;

// Verifica que las once entidades de dominio de BAS-5 migran correctamente contra
// una base de datos real y que las restricciones únicas de data-model.md se
// aplican de verdad, no solo en el código de configuración de EF Core.
public class DominioEntitiesTests(AppHostSqlFixture fixture) : IClassFixture<AppHostSqlFixture>
{
    private ApplicationDbContext CrearContexto()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(fixture.ConnectionString)
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task LaMigracionSeAplicaSinPendientes()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = CrearContexto();

        var pendientes = await context.Database.GetPendingMigrationsAsync(cancellationToken);

        Assert.Empty(pendientes);
    }

    [Fact]
    public async Task CompeticionRechazaTemporadaYCategoriaDuplicadas()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = CrearContexto();

        var temporada = new Temporada { Nombre = "2025-2026 (test-competicion)", FechaInicio = new DateOnly(2025, 9, 1), FechaFin = new DateOnly(2026, 6, 30) };
        var categoria = new Categoria { Nombre = "Cadete (test-competicion)", Orden = 4 };
        context.AddRange(temporada, categoria);
        await context.SaveChangesAsync(cancellationToken);

        context.Add(new Competicion { TemporadaId = temporada.Id, CategoriaId = categoria.Id });
        await context.SaveChangesAsync(cancellationToken);

        context.Add(new Competicion { TemporadaId = temporada.Id, CategoriaId = categoria.Id });
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync(cancellationToken));
    }

    [Fact]
    public async Task JornadaRechazaNumeroDuplicadoEnLaMismaCompeticion()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = CrearContexto();

        var temporada = new Temporada { Nombre = "2025-2026 (test-jornada)", FechaInicio = new DateOnly(2025, 9, 1), FechaFin = new DateOnly(2026, 6, 30) };
        var categoria = new Categoria { Nombre = "Infantil (test-jornada)", Orden = 3 };
        context.AddRange(temporada, categoria);
        await context.SaveChangesAsync(cancellationToken);

        var competicion = new Competicion { TemporadaId = temporada.Id, CategoriaId = categoria.Id };
        context.Add(competicion);
        await context.SaveChangesAsync(cancellationToken);

        context.Add(new Jornada { CompeticionId = competicion.Id, Numero = 1 });
        await context.SaveChangesAsync(cancellationToken);

        context.Add(new Jornada { CompeticionId = competicion.Id, Numero = 1 });
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync(cancellationToken));
    }

    [Fact]
    public async Task FichaJugadorRechazaDorsalDuplicadoEnElMismoEquipo()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var context = CrearContexto();

        var temporada = new Temporada { Nombre = "2025-2026 (test-ficha)", FechaInicio = new DateOnly(2025, 9, 1), FechaFin = new DateOnly(2026, 6, 30) };
        var categoria = new Categoria { Nombre = "Alevín (test-ficha)", Orden = 2 };
        var club = new Club { Nombre = "CB Test", Municipio = "Sevilla", FechaAlta = new DateOnly(2020, 1, 1) };
        context.AddRange(temporada, categoria, club);
        await context.SaveChangesAsync(cancellationToken);

        var competicion = new Competicion { TemporadaId = temporada.Id, CategoriaId = categoria.Id };
        context.Add(competicion);
        await context.SaveChangesAsync(cancellationToken);

        var equipo = new Equipo { CompeticionId = competicion.Id, ClubId = club.Id, Nombre = "CB Test A", Estado = EquipoEstado.Activo };
        context.Add(equipo);
        await context.SaveChangesAsync(cancellationToken);

        context.Add(new FichaJugador { EquipoId = equipo.Id, Dorsal = 7 });
        await context.SaveChangesAsync(cancellationToken);

        context.Add(new FichaJugador { EquipoId = equipo.Id, Dorsal = 7 });
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync(cancellationToken));
    }
}
