namespace BasketBaseTracker.Web.Domain;

public readonly record struct FilaClasificacion(
    int EquipoId,
    int PartidosJugados,
    int Victorias,
    int Derrotas,
    int PuntosFavor,
    int PuntosContra,
    int PuntosClasificacion)
{
    public int DiferenciaDeTantos => PuntosFavor - PuntosContra;

    // 0 en vez de NaN/excepción cuando un equipo no ha jugado ningún partido
    // (PuntosContra = 0), para que el desempate no rompa al equipo que va por
    // detrás de cualquiera con cociente positivo.
    public double CocienteDeTantos =>
        PuntosContra == 0 ? (PuntosFavor > 0 ? double.PositiveInfinity : 0) : (double)PuntosFavor / PuntosContra;
}

// Agrega Partido (ya jugados/resueltos) y PenalizacionClasificacion por equipo y
// ordena la tabla — desempate limitado a los criterios que no dependen de en qué
// fase está la competición (data-model.md, architecture.md punto 14; ver
// spec.md de BAS-11 para el resto, fuera de alcance).
public static class ClasificacionCalculator
{
    public static IReadOnlyList<FilaClasificacion> Calcular(
        IEnumerable<int> equipoIds,
        IEnumerable<(int EquipoLocalId, int EquipoVisitanteId, int PuntosLocal, int PuntosVisitante)> partidos,
        IEnumerable<(int EquipoId, int Puntos)> penalizaciones,
        int puntosVictoria,
        int puntosDerrota)
    {
        var partidosList = partidos.ToList();
        var penalizacionesPorEquipo = penalizaciones
            .GroupBy(p => p.EquipoId)
            .ToDictionary(g => g.Key, g => g.Sum(p => p.Puntos));

        return equipoIds
            .Select(equipoId => CalcularFila(equipoId, partidosList, penalizacionesPorEquipo, puntosVictoria, puntosDerrota))
            .OrderByDescending(f => f.PuntosClasificacion)
            .ThenByDescending(f => f.DiferenciaDeTantos)
            .ThenByDescending(f => f.CocienteDeTantos)
            .ToList();
    }

    private static FilaClasificacion CalcularFila(
        int equipoId,
        List<(int EquipoLocalId, int EquipoVisitanteId, int PuntosLocal, int PuntosVisitante)> partidos,
        Dictionary<int, int> penalizacionesPorEquipo,
        int puntosVictoria,
        int puntosDerrota)
    {
        var jugados = partidos
            .Where(p => p.EquipoLocalId == equipoId || p.EquipoVisitanteId == equipoId)
            .Select(p => p.EquipoLocalId == equipoId
                ? (Favor: p.PuntosLocal, Contra: p.PuntosVisitante)
                : (Favor: p.PuntosVisitante, Contra: p.PuntosLocal))
            .ToList();

        var victorias = jugados.Count(j => j.Favor > j.Contra);
        var derrotas = jugados.Count - victorias;
        var puntosClasificacion = (victorias * puntosVictoria) + (derrotas * puntosDerrota)
            + penalizacionesPorEquipo.GetValueOrDefault(equipoId);

        return new FilaClasificacion(
            equipoId, jugados.Count, victorias, derrotas,
            jugados.Sum(j => j.Favor), jugados.Sum(j => j.Contra), puntosClasificacion);
    }
}
