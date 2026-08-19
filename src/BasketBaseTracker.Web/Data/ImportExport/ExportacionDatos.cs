using BasketBaseTracker.Web.Data.Entities;

namespace BasketBaseTracker.Web.Data.ImportExport;

// DTOs de exportación explícitos, no las entidades EF directamente — desacopla
// el formato del fichero de futuros cambios de esquema y evita ciclos de
// navegación al serializar (BAS-16, plan.md). Solo campos escalares y FK; sin
// RowVersion (columna rowversion de SQL Server, gestionada por el motor, no se
// puede fijar por INSERT).

public record SedeExport(int Id, string Nombre, string Municipio, string Direccion);

public record ClubExport(int Id, string Nombre, string Municipio, DateOnly FechaAlta);

public record TemporadaExport(int Id, string Nombre, DateOnly FechaInicio, DateOnly FechaFin, TemporadaEstado Estado);

public record CategoriaExport(int Id, string Nombre, int Orden);

public record CompeticionExport(int Id, int TemporadaId, int CategoriaId, int PuntosVictoria, int PuntosDerrota);

public record EquipoExport(int Id, int CompeticionId, int ClubId, string Nombre, int? SedeHabitualId, EquipoEstado Estado);

public record FichaJugadorExport(int Id, int EquipoId, int Dorsal, Posicion? Posicion);

public record JornadaExport(int Id, int CompeticionId, int Numero, string? Etiqueta, bool CuentaParaClasificacion);

public record PartidoExport(
    int Id,
    int JornadaId,
    int EquipoLocalId,
    int EquipoVisitanteId,
    int? SedeId,
    DateTime? FechaHora,
    PartidoEstado Estado,
    int? PuntosLocal,
    int? PuntosVisitante,
    MotivoResolucion? MotivoResolucion,
    int? EquipoGanadorResolucionId,
    string? Observaciones);

public record PartidoParcialExport(int Id, int PartidoId, int NumeroPeriodo, int PuntosLocal, int PuntosVisitante);

public record PenalizacionClasificacionExport(int Id, int EquipoId, int Puntos, string Motivo, int? PartidoId, DateOnly FechaAplicacion);

public record ExportacionDatos(
    int SchemaVersion,
    IReadOnlyList<SedeExport> Sedes,
    IReadOnlyList<ClubExport> Clubes,
    IReadOnlyList<TemporadaExport> Temporadas,
    IReadOnlyList<CategoriaExport> Categorias,
    IReadOnlyList<CompeticionExport> Competiciones,
    IReadOnlyList<EquipoExport> Equipos,
    IReadOnlyList<FichaJugadorExport> FichasJugador,
    IReadOnlyList<JornadaExport> Jornadas,
    IReadOnlyList<PartidoExport> Partidos,
    IReadOnlyList<PartidoParcialExport> PartidoParciales,
    IReadOnlyList<PenalizacionClasificacionExport> Penalizaciones);
