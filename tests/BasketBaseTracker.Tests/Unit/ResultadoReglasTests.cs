using BasketBaseTracker.Web.Data.Entities;
using BasketBaseTracker.Web.Domain;

namespace BasketBaseTracker.Tests.Unit;

public class ResultadoReglasTests
{
    [Theory]
    [InlineData(PartidoEstado.Jugado, true)]
    [InlineData(PartidoEstado.Resuelto, true)]
    [InlineData(PartidoEstado.Programado, false)]
    [InlineData(PartidoEstado.Aplazado, false)]
    [InlineData(PartidoEstado.Cancelado, false)]
    public void RequiereMarcador_SoloParaJugadoYResuelto(PartidoEstado estado, bool esperado)
    {
        Assert.Equal(esperado, ResultadoReglas.RequiereMarcador(estado));
    }

    [Fact]
    public void MarcadorValido_DevuelveFalso_SiHayEmpate()
    {
        Assert.False(ResultadoReglas.MarcadorValido(80, 80));
    }

    [Fact]
    public void MarcadorValido_DevuelveVerdadero_SinEmpate()
    {
        Assert.True(ResultadoReglas.MarcadorValido(85, 80));
    }

    [Theory]
    [InlineData(null, 80)]
    [InlineData(80, null)]
    [InlineData(null, null)]
    public void MarcadorValido_DevuelveFalso_SiFaltaAlgunPunto(int? local, int? visitante)
    {
        Assert.False(ResultadoReglas.MarcadorValido(local, visitante));
    }

    [Fact]
    public void ResolucionValida_DevuelveVerdadero_ConMotivoYGanadorDelPartido()
    {
        var resultado = ResultadoReglas.ResolucionValida(MotivoResolucion.Incomparecencia, equipoGanadorId: 5, equipoLocalId: 5, equipoVisitanteId: 6);

        Assert.True(resultado);
    }

    [Fact]
    public void ResolucionValida_DevuelveFalso_SinMotivo()
    {
        var resultado = ResultadoReglas.ResolucionValida(null, equipoGanadorId: 5, equipoLocalId: 5, equipoVisitanteId: 6);

        Assert.False(resultado);
    }

    [Fact]
    public void ResolucionValida_DevuelveFalso_SinGanador()
    {
        var resultado = ResultadoReglas.ResolucionValida(MotivoResolucion.Otro, equipoGanadorId: null, equipoLocalId: 5, equipoVisitanteId: 6);

        Assert.False(resultado);
    }

    [Fact]
    public void ResolucionValida_DevuelveFalso_SiElGanadorNoEsNingunoDeLosDosEquipos()
    {
        var resultado = ResultadoReglas.ResolucionValida(MotivoResolucion.Otro, equipoGanadorId: 99, equipoLocalId: 5, equipoVisitanteId: 6);

        Assert.False(resultado);
    }

    [Fact]
    public void MarcadorTecnicoSugerido_DosACeroSiGanaElLocal()
    {
        var resultado = ResultadoReglas.MarcadorTecnicoSugerido(equipoGanadorId: 5, equipoLocalId: 5);

        Assert.Equal((2, 0), resultado);
    }

    [Fact]
    public void MarcadorTecnicoSugerido_CeroADosSiGanaElVisitante()
    {
        var resultado = ResultadoReglas.MarcadorTecnicoSugerido(equipoGanadorId: 6, equipoLocalId: 5);

        Assert.Equal((0, 2), resultado);
    }

    [Fact]
    public void SumarParciales_SumaTodosLosPeriodos()
    {
        (int, int)[] parciales = [(20, 18), (22, 20), (18, 19), (15, 17)];

        var resultado = ResultadoReglas.SumarParciales(parciales);

        Assert.Equal((75, 74), resultado);
    }

    [Fact]
    public void SumarParciales_SinParciales_DevuelveCero()
    {
        var resultado = ResultadoReglas.SumarParciales([]);

        Assert.Equal((0, 0), resultado);
    }

    [Fact]
    public void SumarParciales_IncluyeProrrogaSinTratamientoEspecial()
    {
        // Periodos 1-4 más una prórroga (NumeroPeriodo 5) — se suman igual,
        // sin distinguir prórroga de tiempo reglamentario (spec.md).
        (int, int)[] parciales = [(20, 18), (22, 20), (18, 19), (15, 17), (8, 6)];

        var resultado = ResultadoReglas.SumarParciales(parciales);

        Assert.Equal((83, 80), resultado);
    }
}
