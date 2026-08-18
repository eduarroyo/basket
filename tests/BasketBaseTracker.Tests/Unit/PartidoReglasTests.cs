using BasketBaseTracker.Web.Domain;

namespace BasketBaseTracker.Tests.Unit;

public class PartidoReglasTests
{
    [Fact]
    public void EquipoYaJuegaEnJornada_DevuelveFalso_SinPartidosPrevios()
    {
        var resultado = PartidoReglas.EquipoYaJuegaEnJornada([], partidoIdActual: 0, equipoLocalId: 1, equipoVisitanteId: 2);

        Assert.False(resultado);
    }

    [Fact]
    public void EquipoYaJuegaEnJornada_DevuelveVerdadero_SiElEquipoLocalYaEsLocalEnOtroPartido()
    {
        (int PartidoId, int EquipoLocalId, int EquipoVisitanteId)[] partidos = [(1, 5, 6)];

        var resultado = PartidoReglas.EquipoYaJuegaEnJornada(partidos, partidoIdActual: 0, equipoLocalId: 5, equipoVisitanteId: 7);

        Assert.True(resultado);
    }

    [Fact]
    public void EquipoYaJuegaEnJornada_DevuelveVerdadero_SiElEquipoLocalYaEsVisitanteEnOtroPartido()
    {
        (int PartidoId, int EquipoLocalId, int EquipoVisitanteId)[] partidos = [(1, 6, 5)];

        var resultado = PartidoReglas.EquipoYaJuegaEnJornada(partidos, partidoIdActual: 0, equipoLocalId: 5, equipoVisitanteId: 7);

        Assert.True(resultado);
    }

    [Fact]
    public void EquipoYaJuegaEnJornada_ExcluyeElPropioPartidoActual_AlEditar()
    {
        (int PartidoId, int EquipoLocalId, int EquipoVisitanteId)[] partidos = [(1, 5, 6)];

        var resultado = PartidoReglas.EquipoYaJuegaEnJornada(partidos, partidoIdActual: 1, equipoLocalId: 5, equipoVisitanteId: 6);

        Assert.False(resultado);
    }
}
