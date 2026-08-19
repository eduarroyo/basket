using BasketBaseTracker.Seed;
using BasketBaseTracker.Web.Data;
using BasketBaseTracker.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BasketBaseTracker.Tests.Integration;

// Clase dedicada, sin otros [Fact] compartiendo la fixture: el seeder de
// demostración escribe un volumen de datos considerable y su propio --reset
// borra toda la base de datos deportiva — mismo motivo de aislamiento que
// MigracionRolesAdminTests e ImportExportReemplazoCompletoTests.
public class DemoSeederTests(AppHostSqlFixture fixture) : IClassFixture<AppHostSqlFixture>
{
    private static ApplicationDbContext CrearContexto(string connectionString) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connectionString).Options);

    [Fact]
    public async Task SeedAsync_PueblaUnDatasetCoherenteYRespetaLasSalvaguardas()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        // Con 6 clubes (par), la ida y vuelta genera 10 jornadas repartidas en fines
        // de semana consecutivos desde el primer sábado de septiembre — mediados de
        // octubre cae a mitad de esa ventana, dejando jornadas ya jugadas y otras
        // todavía futuras.
        var hoy = new DateOnly(2026, 10, 15);
        await using var context = CrearContexto(fixture.ConnectionString);

        await DemoSeeder.SeedAsync(context, new SeedOptions(Temporadas: 1, Clubes: 6, Reset: false), hoy, new Random(1), cancellationToken);

        Assert.True(await context.Categorias.AnyAsync(cancellationToken));
        Assert.True(await context.Sedes.AnyAsync(cancellationToken));
        Assert.True(await context.Clubes.AnyAsync(cancellationToken));
        Assert.True(await context.Equipos.AnyAsync(cancellationToken));
        Assert.True(await context.FichasJugador.AnyAsync(cancellationToken));
        Assert.True(await context.Jornadas.AnyAsync(cancellationToken));
        Assert.True(await context.Partidos.AnyAsync(cancellationToken));

        var temporadas = await context.Temporadas.ToListAsync(cancellationToken);
        var temporadaHistorica = Assert.Single(temporadas, t => t.Estado is TemporadaEstado.Finalizada or TemporadaEstado.Archivada);
        var temporadaEnCurso = Assert.Single(temporadas, t => t.Estado == TemporadaEstado.EnCurso);

        var partidosHistoricos = await context.Partidos
            .Where(p => p.Jornada.Competicion.TemporadaId == temporadaHistorica.Id)
            .ToListAsync(cancellationToken);
        Assert.NotEmpty(partidosHistoricos);
        Assert.All(partidosHistoricos, p =>
        {
            Assert.Equal(PartidoEstado.Jugado, p.Estado);
            Assert.NotNull(p.PuntosLocal);
            Assert.NotNull(p.PuntosVisitante);
        });
        var partidoIdsHistoricos = partidosHistoricos.Select(p => p.Id).ToHashSet();
        var parcialesHistoricos = await context.PartidosParciales
            .Where(pp => partidoIdsHistoricos.Contains(pp.PartidoId))
            .ToListAsync(cancellationToken);
        Assert.Equal(partidosHistoricos.Count * 4, parcialesHistoricos.Count);

        var partidosEnCurso = await context.Partidos
            .Where(p => p.Jornada.Competicion.TemporadaId == temporadaEnCurso.Id)
            .ToListAsync(cancellationToken);
        Assert.Contains(partidosEnCurso, p => p.Estado == PartidoEstado.Jugado && p.PuntosLocal != null);
        Assert.Contains(partidosEnCurso, p => p.Estado == PartidoEstado.Programado && p.PuntosLocal == null);

        var partidosPorJornada = (await context.Partidos
            .Select(p => new { p.JornadaId, p.EquipoLocalId, p.EquipoVisitanteId })
            .ToListAsync(cancellationToken))
            .GroupBy(p => p.JornadaId);
        foreach (var jornada in partidosPorJornada)
        {
            var equiposEnLaJornada = jornada.SelectMany(p => new[] { p.EquipoLocalId, p.EquipoVisitanteId }).ToList();
            Assert.Equal(equiposEnLaJornada.Count, equiposEnLaJornada.Distinct().Count());
        }

        // Salvaguarda: sin --reset, volver a sembrar sobre datos existentes falla
        // sin tocar nada.
        var totalEquiposAntes = await context.Equipos.CountAsync(cancellationToken);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            DemoSeeder.SeedAsync(context, new SeedOptions(Temporadas: 1, Clubes: 6, Reset: false), hoy, new Random(2), cancellationToken));
        Assert.Equal(totalEquiposAntes, await context.Equipos.CountAsync(cancellationToken));

        // Con --reset, sustituye el dataset por uno nuevo (distinto nº de clubes
        // para comprobar que realmente se ha regenerado, no acumulado).
        await DemoSeeder.SeedAsync(context, new SeedOptions(Temporadas: 1, Clubes: 9, Reset: true), hoy, new Random(3), cancellationToken);
        var clubesTrasReset = await context.Clubes.CountAsync(cancellationToken);
        Assert.Equal(9, clubesTrasReset);
    }
}
