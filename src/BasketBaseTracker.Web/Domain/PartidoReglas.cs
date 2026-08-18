namespace BasketBaseTracker.Web.Domain;

// Invariante de aplicación no expresable como constraint de BD (data-model.md):
// un equipo no puede aparecer dos veces en la misma jornada, ni como local ni
// como visitante.
public static class PartidoReglas
{
    public static bool EquipoYaJuegaEnJornada(
        IEnumerable<(int PartidoId, int EquipoLocalId, int EquipoVisitanteId)> partidosDeLaJornada,
        int partidoIdActual,
        int equipoLocalId,
        int equipoVisitanteId)
    {
        return partidosDeLaJornada.Any(p =>
            p.PartidoId != partidoIdActual &&
            (p.EquipoLocalId == equipoLocalId || p.EquipoVisitanteId == equipoLocalId ||
             p.EquipoLocalId == equipoVisitanteId || p.EquipoVisitanteId == equipoVisitanteId));
    }
}
