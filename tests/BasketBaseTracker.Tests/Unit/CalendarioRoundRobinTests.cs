using BasketBaseTracker.Seed.Dominio;

namespace BasketBaseTracker.Tests.Unit;

public class CalendarioRoundRobinTests
{
    [Theory]
    [InlineData(8)]
    [InlineData(7)]
    [InlineData(2)]
    public void GenerarJornadas_UnaVuelta_NingunEquipoSeRepiteEnLaMismaJornada(int numeroDeEquipos)
    {
        var equipos = Enumerable.Range(1, numeroDeEquipos).ToList();

        var jornadas = CalendarioRoundRobin.GenerarJornadas(equipos, dobleVuelta: false);

        foreach (var jornada in jornadas)
        {
            var equiposEnLaJornada = jornada.SelectMany(p => new[] { p.EquipoLocalId, p.EquipoVisitanteId }).ToList();
            Assert.Equal(equiposEnLaJornada.Count, equiposEnLaJornada.Distinct().Count());
        }
    }

    [Theory]
    [InlineData(8, 7)]
    [InlineData(7, 7)]
    [InlineData(6, 5)]
    public void GenerarJornadas_UnaVuelta_GeneraElNumeroDeJornadasEsperado(int numeroDeEquipos, int jornadasEsperadas)
    {
        var equipos = Enumerable.Range(1, numeroDeEquipos).ToList();

        var jornadas = CalendarioRoundRobin.GenerarJornadas(equipos, dobleVuelta: false);

        Assert.Equal(jornadasEsperadas, jornadas.Count);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(7)]
    public void GenerarJornadas_UnaVuelta_CadaParDeEquiposSeEnfrentaExactamenteUnaVez(int numeroDeEquipos)
    {
        var equipos = Enumerable.Range(1, numeroDeEquipos).ToList();

        var jornadas = CalendarioRoundRobin.GenerarJornadas(equipos, dobleVuelta: false);

        var totalPartidos = jornadas.Sum(j => j.Count);
        Assert.Equal(numeroDeEquipos * (numeroDeEquipos - 1) / 2, totalPartidos);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(7)]
    public void GenerarJornadas_DobleVuelta_DuplicaJornadasYPartidosInvirtiendoLocalVisitante(int numeroDeEquipos)
    {
        var equipos = Enumerable.Range(1, numeroDeEquipos).ToList();

        var unaVuelta = CalendarioRoundRobin.GenerarJornadas(equipos, dobleVuelta: false);
        var dobleVuelta = CalendarioRoundRobin.GenerarJornadas(equipos, dobleVuelta: true);

        Assert.Equal(unaVuelta.Count * 2, dobleVuelta.Count);
        var totalPartidos = dobleVuelta.Sum(j => j.Count);
        Assert.Equal(numeroDeEquipos * (numeroDeEquipos - 1), totalPartidos);

        var segundaVuelta = dobleVuelta.Skip(unaVuelta.Count).ToList();
        var emparejamientosInvertidos = segundaVuelta
            .SelectMany(j => j)
            .Select(p => (p.EquipoVisitanteId, p.EquipoLocalId))
            .ToHashSet();
        var emparejamientosOriginales = unaVuelta
            .SelectMany(j => j)
            .Select(p => (p.EquipoLocalId, p.EquipoVisitanteId))
            .ToHashSet();
        Assert.Equal(emparejamientosOriginales, emparejamientosInvertidos);
    }

    [Fact]
    public void GenerarJornadas_ConMenosDeDosEquipos_NoGeneraJornadas()
    {
        Assert.Empty(CalendarioRoundRobin.GenerarJornadas([1], dobleVuelta: false));
        Assert.Empty(CalendarioRoundRobin.GenerarJornadas([], dobleVuelta: true));
    }
}
