using BasketBaseTracker.Web.Domain;

namespace BasketBaseTracker.Tests.Unit;

public class ClasificacionCalculatorTests
{
    private const int EquipoA = 1;
    private const int EquipoB = 2;
    private const int EquipoC = 3;

    [Fact]
    public void OrdenaPorPuntosDeClasificacion()
    {
        (int, int, int, int)[] partidos = [(EquipoA, EquipoB, 80, 70)]; // A gana

        var tabla = ClasificacionCalculator.Calcular([EquipoA, EquipoB], partidos, [], puntosVictoria: 2, puntosDerrota: 1);

        Assert.Equal(EquipoA, tabla[0].EquipoId);
        Assert.Equal(2, tabla[0].PuntosClasificacion);
        Assert.Equal(EquipoB, tabla[1].EquipoId);
        Assert.Equal(1, tabla[1].PuntosClasificacion);
    }

    [Fact]
    public void DesempataPorDiferenciaGeneralDeTantosCuandoLosPuntosCoinciden()
    {
        // A y B tienen 1 victoria y 1 derrota cada uno (2+1 = 3 puntos), pero A
        // tiene mejor diferencia de tantos.
        (int, int, int, int)[] partidos =
        [
            (EquipoA, EquipoC, 100, 50), // A gana con mucha diferencia
            (EquipoC, EquipoA, 80, 70), // A pierde por poco
            (EquipoB, EquipoC, 70, 60), // B gana por poco
            (EquipoC, EquipoB, 90, 60), // B pierde con mucha diferencia
        ];

        var tabla = ClasificacionCalculator.Calcular([EquipoA, EquipoB], partidos, [], puntosVictoria: 2, puntosDerrota: 1);

        var filaA = tabla.Single(f => f.EquipoId == EquipoA);
        var filaB = tabla.Single(f => f.EquipoId == EquipoB);

        Assert.Equal(filaA.PuntosClasificacion, filaB.PuntosClasificacion);
        Assert.True(filaA.DiferenciaDeTantos > filaB.DiferenciaDeTantos);
        Assert.Equal(EquipoA, tabla[0].EquipoId);
    }

    [Fact]
    public void DesempataPorCocienteDeTantosCuandoLaDiferenciaTambienCoincide()
    {
        // A: 100 a favor, 90 en contra (diferencia +10, cociente 1,11).
        // B: 40 a favor, 30 en contra (diferencia +10, cociente 1,33).
        (int, int, int, int)[] partidos =
        [
            (EquipoA, EquipoC, 100, 90),
            (EquipoB, EquipoC, 40, 30),
        ];

        var tabla = ClasificacionCalculator.Calcular([EquipoA, EquipoB], partidos, [], puntosVictoria: 2, puntosDerrota: 1);

        Assert.Equal(EquipoB, tabla[0].EquipoId);
        Assert.Equal(EquipoA, tabla[1].EquipoId);
    }

    [Fact]
    public void LaPenalizacionRestaPuntosDeClasificacion()
    {
        (int, int, int, int)[] partidos = [(EquipoA, EquipoB, 80, 70)];
        (int, int)[] penalizaciones = [(EquipoA, -1)];

        var tabla = ClasificacionCalculator.Calcular([EquipoA, EquipoB], partidos, penalizaciones, puntosVictoria: 2, puntosDerrota: 1);

        var filaA = tabla.Single(f => f.EquipoId == EquipoA);
        Assert.Equal(1, filaA.PuntosClasificacion);
    }

    [Fact]
    public void UnEquipoSinPartidosApareceConTodoACero()
    {
        var tabla = ClasificacionCalculator.Calcular([EquipoA], [], [], puntosVictoria: 2, puntosDerrota: 1);

        var fila = Assert.Single(tabla);
        Assert.Equal(0, fila.PartidosJugados);
        Assert.Equal(0, fila.Victorias);
        Assert.Equal(0, fila.Derrotas);
        Assert.Equal(0, fila.PuntosFavor);
        Assert.Equal(0, fila.PuntosContra);
        Assert.Equal(0, fila.PuntosClasificacion);
        Assert.Equal(0, fila.CocienteDeTantos);
    }
}
