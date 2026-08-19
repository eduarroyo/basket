using BasketBaseTracker.Web.Data.Entities;
using BasketBaseTracker.Web.Data.ImportExport;

namespace BasketBaseTracker.Web.Domain;

// Exportación e importación completa de todos los datos del sistema
// (architecture.md punto 10) — reemplazo total, no fusión. Exportar/Validar son
// funciones puras (sin base de datos), testeables en aislamiento; ImportarAsync
// (que sí toca la base de datos) vive en la página que la invoca, ver plan.md.
public static class ImportExportService
{
    public const int SchemaVersionActual = 1;

    public static ExportacionDatos Exportar(
        IReadOnlyList<Sede> sedes,
        IReadOnlyList<Club> clubes,
        IReadOnlyList<Temporada> temporadas,
        IReadOnlyList<Categoria> categorias,
        IReadOnlyList<Competicion> competiciones,
        IReadOnlyList<Equipo> equipos,
        IReadOnlyList<FichaJugador> fichasJugador,
        IReadOnlyList<Jornada> jornadas,
        IReadOnlyList<Partido> partidos,
        IReadOnlyList<PartidoParcial> partidoParciales,
        IReadOnlyList<PenalizacionClasificacion> penalizaciones) => new(
        SchemaVersionActual,
        sedes.Select(s => new SedeExport(s.Id, s.Nombre, s.Municipio, s.Direccion)).ToList(),
        clubes.Select(c => new ClubExport(c.Id, c.Nombre, c.Municipio, c.FechaAlta)).ToList(),
        temporadas.Select(t => new TemporadaExport(t.Id, t.Nombre, t.FechaInicio, t.FechaFin, t.Estado)).ToList(),
        categorias.Select(c => new CategoriaExport(c.Id, c.Nombre, c.Orden)).ToList(),
        competiciones.Select(c => new CompeticionExport(c.Id, c.TemporadaId, c.CategoriaId, c.PuntosVictoria, c.PuntosDerrota)).ToList(),
        equipos.Select(e => new EquipoExport(e.Id, e.CompeticionId, e.ClubId, e.Nombre, e.SedeHabitualId, e.Estado)).ToList(),
        fichasJugador.Select(f => new FichaJugadorExport(f.Id, f.EquipoId, f.Dorsal, f.Posicion)).ToList(),
        jornadas.Select(j => new JornadaExport(j.Id, j.CompeticionId, j.Numero, j.Etiqueta, j.CuentaParaClasificacion)).ToList(),
        partidos.Select(p => new PartidoExport(
            p.Id, p.JornadaId, p.EquipoLocalId, p.EquipoVisitanteId, p.SedeId, p.FechaHora, p.Estado,
            p.PuntosLocal, p.PuntosVisitante, p.MotivoResolucion, p.EquipoGanadorResolucionId, p.Observaciones)).ToList(),
        partidoParciales.Select(pp => new PartidoParcialExport(pp.Id, pp.PartidoId, pp.NumeroPeriodo, pp.PuntosLocal, pp.PuntosVisitante)).ToList(),
        penalizaciones.Select(p => new PenalizacionClasificacionExport(p.Id, p.EquipoId, p.Puntos, p.Motivo, p.PartidoId, p.FechaAplicacion)).ToList());

    public static IReadOnlyList<string> Validar(ExportacionDatos datos)
    {
        if (datos.SchemaVersion != SchemaVersionActual)
        {
            return [$"Versión de esquema no soportada: {datos.SchemaVersion} (esperada {SchemaVersionActual})."];
        }

        List<string> errores = [];

        var idsSedes = datos.Sedes.Select(s => s.Id).ToHashSet();
        var idsClubes = datos.Clubes.Select(c => c.Id).ToHashSet();
        var idsTemporadas = datos.Temporadas.Select(t => t.Id).ToHashSet();
        var idsCategorias = datos.Categorias.Select(c => c.Id).ToHashSet();
        var idsCompeticiones = datos.Competiciones.Select(c => c.Id).ToHashSet();
        var idsEquipos = datos.Equipos.Select(e => e.Id).ToHashSet();
        var idsJornadas = datos.Jornadas.Select(j => j.Id).ToHashSet();
        var idsPartidos = datos.Partidos.Select(p => p.Id).ToHashSet();

        foreach (var c in datos.Competiciones)
        {
            if (!idsTemporadas.Contains(c.TemporadaId))
            {
                errores.Add($"Competicion {c.Id}: TemporadaId {c.TemporadaId} no existe en el fichero.");
            }

            if (!idsCategorias.Contains(c.CategoriaId))
            {
                errores.Add($"Competicion {c.Id}: CategoriaId {c.CategoriaId} no existe en el fichero.");
            }
        }

        foreach (var e in datos.Equipos)
        {
            if (!idsCompeticiones.Contains(e.CompeticionId))
            {
                errores.Add($"Equipo {e.Id}: CompeticionId {e.CompeticionId} no existe en el fichero.");
            }

            if (!idsClubes.Contains(e.ClubId))
            {
                errores.Add($"Equipo {e.Id}: ClubId {e.ClubId} no existe en el fichero.");
            }

            if (e.SedeHabitualId is int sedeHabitualId && !idsSedes.Contains(sedeHabitualId))
            {
                errores.Add($"Equipo {e.Id}: SedeHabitualId {sedeHabitualId} no existe en el fichero.");
            }
        }

        foreach (var f in datos.FichasJugador)
        {
            if (!idsEquipos.Contains(f.EquipoId))
            {
                errores.Add($"FichaJugador {f.Id}: EquipoId {f.EquipoId} no existe en el fichero.");
            }
        }

        foreach (var j in datos.Jornadas)
        {
            if (!idsCompeticiones.Contains(j.CompeticionId))
            {
                errores.Add($"Jornada {j.Id}: CompeticionId {j.CompeticionId} no existe en el fichero.");
            }
        }

        foreach (var p in datos.Partidos)
        {
            if (!idsJornadas.Contains(p.JornadaId))
            {
                errores.Add($"Partido {p.Id}: JornadaId {p.JornadaId} no existe en el fichero.");
            }

            if (!idsEquipos.Contains(p.EquipoLocalId))
            {
                errores.Add($"Partido {p.Id}: EquipoLocalId {p.EquipoLocalId} no existe en el fichero.");
            }

            if (!idsEquipos.Contains(p.EquipoVisitanteId))
            {
                errores.Add($"Partido {p.Id}: EquipoVisitanteId {p.EquipoVisitanteId} no existe en el fichero.");
            }

            if (p.SedeId is int sedeId && !idsSedes.Contains(sedeId))
            {
                errores.Add($"Partido {p.Id}: SedeId {sedeId} no existe en el fichero.");
            }

            if (p.EquipoGanadorResolucionId is int equipoGanadorId && !idsEquipos.Contains(equipoGanadorId))
            {
                errores.Add($"Partido {p.Id}: EquipoGanadorResolucionId {equipoGanadorId} no existe en el fichero.");
            }
        }

        foreach (var pp in datos.PartidoParciales)
        {
            if (!idsPartidos.Contains(pp.PartidoId))
            {
                errores.Add($"PartidoParcial {pp.Id}: PartidoId {pp.PartidoId} no existe en el fichero.");
            }
        }

        foreach (var pen in datos.Penalizaciones)
        {
            if (!idsEquipos.Contains(pen.EquipoId))
            {
                errores.Add($"PenalizacionClasificacion {pen.Id}: EquipoId {pen.EquipoId} no existe en el fichero.");
            }

            if (pen.PartidoId is int partidoId && !idsPartidos.Contains(partidoId))
            {
                errores.Add($"PenalizacionClasificacion {pen.Id}: PartidoId {partidoId} no existe en el fichero.");
            }
        }

        // Restricciones únicas de data-model.md, comprobadas aquí porque el
        // fichero se valida entero antes de tocar la base de datos (no basta con
        // dejar que el índice único de SQL Server rechace el INSERT a mitad).
        foreach (var grupo in datos.Competiciones.GroupBy(c => (c.TemporadaId, c.CategoriaId)).Where(g => g.Count() > 1))
        {
            errores.Add($"Competicion duplicada para (TemporadaId={grupo.Key.TemporadaId}, CategoriaId={grupo.Key.CategoriaId}).");
        }

        foreach (var grupo in datos.FichasJugador.GroupBy(f => (f.EquipoId, f.Dorsal)).Where(g => g.Count() > 1))
        {
            errores.Add($"FichaJugador duplicada para (EquipoId={grupo.Key.EquipoId}, Dorsal={grupo.Key.Dorsal}).");
        }

        foreach (var grupo in datos.Jornadas.GroupBy(j => (j.CompeticionId, j.Numero)).Where(g => g.Count() > 1))
        {
            errores.Add($"Jornada duplicada para (CompeticionId={grupo.Key.CompeticionId}, Numero={grupo.Key.Numero}).");
        }

        foreach (var grupo in datos.PartidoParciales.GroupBy(pp => (pp.PartidoId, pp.NumeroPeriodo)).Where(g => g.Count() > 1))
        {
            errores.Add($"PartidoParcial duplicado para (PartidoId={grupo.Key.PartidoId}, NumeroPeriodo={grupo.Key.NumeroPeriodo}).");
        }

        // Invariante de negocio: un equipo no puede aparecer dos veces en la misma
        // jornada (reutiliza PartidoReglas, ya existente desde BAS-9).
        foreach (var grupoPorJornada in datos.Partidos.GroupBy(p => p.JornadaId))
        {
            var partidosDeLaJornada = grupoPorJornada
                .Select(p => (p.Id, p.EquipoLocalId, p.EquipoVisitanteId))
                .ToList();

            foreach (var p in grupoPorJornada)
            {
                if (PartidoReglas.EquipoYaJuegaEnJornada(partidosDeLaJornada, p.Id, p.EquipoLocalId, p.EquipoVisitanteId))
                {
                    errores.Add($"Partido {p.Id}: alguno de los dos equipos ya juega otro partido en la jornada {p.JornadaId}.");
                }
            }
        }

        return errores;
    }
}
