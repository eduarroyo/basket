using BasketBaseTracker.Web.Data;
using BasketBaseTracker.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BasketBaseTracker.Web.Domain;

public sealed record FilaClasificacionVista(
    int Posicion,
    string EquipoNombre,
    int PartidosJugados,
    int Victorias,
    int Derrotas,
    int PuntosFavor,
    int PuntosContra,
    int DiferenciaDeTantos,
    int PuntosClasificacion);

// Consulta compartida por la vista admin y la pública (screens.md: "misma
// consulta que la pública, sin caché") — solo la vista pública añade Output
// Caching por encima.
public class ClasificacionService(ApplicationDbContext context)
{
    public async Task<IReadOnlyList<FilaClasificacionVista>?> ObtenerAsync(int competicionId, CancellationToken cancellationToken)
    {
        var competicion = await context.Competiciones.FindAsync([competicionId], cancellationToken);
        if (competicion is null)
        {
            return null;
        }

        var equipos = await context.Equipos
            .Where(e => e.CompeticionId == competicionId)
            .Select(e => new { e.Id, e.Nombre })
            .ToListAsync(cancellationToken);

        // PuntosLocal/PuntosVisitante son nullable en el modelo porque
        // Programado/Aplazado/Cancelado no los tienen, no porque puedan faltar en
        // Jugado/Resuelto (BAS-10, ResultadoReglas.RequiereMarcador ya lo exige al
        // guardar) — el .Value es seguro aquí.
        var partidos = await context.Partidos
            .Where(p => p.Jornada.CompeticionId == competicionId
                && p.Jornada.CuentaParaClasificacion
                && (p.Estado == PartidoEstado.Jugado || p.Estado == PartidoEstado.Resuelto))
            .Select(p => new { p.EquipoLocalId, p.EquipoVisitanteId, PuntosLocal = p.PuntosLocal!.Value, PuntosVisitante = p.PuntosVisitante!.Value })
            .ToListAsync(cancellationToken);

        var equipoIds = equipos.Select(e => e.Id).ToList();
        var penalizaciones = await context.PenalizacionesClasificacion
            .Where(p => equipoIds.Contains(p.EquipoId))
            .Select(p => new { p.EquipoId, p.Puntos })
            .ToListAsync(cancellationToken);

        var filas = ClasificacionCalculator.Calcular(
            equipoIds,
            partidos.Select(p => (p.EquipoLocalId, p.EquipoVisitanteId, p.PuntosLocal, p.PuntosVisitante)),
            penalizaciones.Select(p => (p.EquipoId, p.Puntos)),
            competicion.PuntosVictoria,
            competicion.PuntosDerrota);

        var nombresPorId = equipos.ToDictionary(e => e.Id, e => e.Nombre);

        return filas
            .Select((f, indice) => new FilaClasificacionVista(
                indice + 1, nombresPorId[f.EquipoId], f.PartidosJugados, f.Victorias, f.Derrotas,
                f.PuntosFavor, f.PuntosContra, f.DiferenciaDeTantos, f.PuntosClasificacion))
            .ToList();
    }
}
