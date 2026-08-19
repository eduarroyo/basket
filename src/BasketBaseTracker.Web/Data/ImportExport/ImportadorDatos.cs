using BasketBaseTracker.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BasketBaseTracker.Web.Data.ImportExport;

// Reemplazo completo de todos los datos en alcance (architecture.md punto 10) —
// solo se invoca tras ImportExportService.Validar sin errores. Vive en Data/ (no
// en Domain/, que se mantiene sin dependencias de EF Core) porque manipula el
// DbContext directamente: borrado + inserción con IDENTITY_INSERT en una única
// transacción.
public static class ImportadorDatos
{
    // Orden inverso de dependencia — todas las FK son DeleteBehavior.Restrict, sin
    // cascada (Data/Configurations/), así que hay que borrar los hijos antes que
    // los padres o SQL Server rechaza el DELETE.
    private static readonly string[] TablasEnOrdenDeBorrado =
    [
        "PenalizacionesClasificacion", "PartidosParciales", "Partidos", "Jornadas",
        "FichasJugador", "Equipos", "Competiciones", "Categorias", "Clubes", "Sedes", "Temporadas",
    ];

    public static async Task ImportarAsync(ApplicationDbContext context, ExportacionDatos datos, CancellationToken cancellationToken)
    {
        // El DbContext puede traer entidades ya trackeadas de consultas previas en
        // la misma petición (p. ej. al cargar los datos actuales para el backup) —
        // se limpian para que el borrado/inserción de abajo no arrastre estado
        // obsoleto.
        context.ChangeTracker.Clear();

        // AddSqlServerDbContext configura reintentos ante fallos transitorios
        // (Program.cs, SqlResilienceOptions) — con una estrategia de reintento
        // activa, EF Core exige envolver cualquier transacción iniciada a mano en
        // CreateExecutionStrategy().ExecuteAsync, si no lanza en tiempo de
        // ejecución la primera vez que se prueba (limitación documentada de EF
        // Core, no específica de este proyecto).
        var estrategia = context.Database.CreateExecutionStrategy();
        await estrategia.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

            foreach (var tabla in TablasEnOrdenDeBorrado)
            {
#pragma warning disable EF1002 // "tabla" viene siempre de TablasEnOrdenDeBorrado (constante interna, nunca de entrada de usuario) — SQL Server no permite parametrizar el nombre de una tabla.
                await context.Database.ExecuteSqlRawAsync($"DELETE FROM [{tabla}]", cancellationToken);
#pragma warning restore EF1002
            }

            await InsertarConIdentityInsertAsync(context, "Sedes", datos.Sedes.Select(MapearSede).ToList(), cancellationToken);
            await InsertarConIdentityInsertAsync(context, "Clubes", datos.Clubes.Select(MapearClub).ToList(), cancellationToken);
            await InsertarConIdentityInsertAsync(context, "Temporadas", datos.Temporadas.Select(MapearTemporada).ToList(), cancellationToken);
            await InsertarConIdentityInsertAsync(context, "Categorias", datos.Categorias.Select(MapearCategoria).ToList(), cancellationToken);
            await InsertarConIdentityInsertAsync(context, "Competiciones", datos.Competiciones.Select(MapearCompeticion).ToList(), cancellationToken);
            await InsertarConIdentityInsertAsync(context, "Equipos", datos.Equipos.Select(MapearEquipo).ToList(), cancellationToken);
            await InsertarConIdentityInsertAsync(context, "FichasJugador", datos.FichasJugador.Select(MapearFichaJugador).ToList(), cancellationToken);
            await InsertarConIdentityInsertAsync(context, "Jornadas", datos.Jornadas.Select(MapearJornada).ToList(), cancellationToken);
            await InsertarConIdentityInsertAsync(context, "Partidos", datos.Partidos.Select(MapearPartido).ToList(), cancellationToken);
            await InsertarConIdentityInsertAsync(context, "PartidosParciales", datos.PartidoParciales.Select(MapearPartidoParcial).ToList(), cancellationToken);
            await InsertarConIdentityInsertAsync(context, "PenalizacionesClasificacion", datos.Penalizaciones.Select(MapearPenalizacion).ToList(), cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        });
    }

    // SQL Server solo permite una tabla con IDENTITY_INSERT activo a la vez por
    // conexión — ON antes de insertar esta tabla, OFF justo después.
    private static async Task InsertarConIdentityInsertAsync<TEntity>(
        ApplicationDbContext context, string tabla, List<TEntity> entidades, CancellationToken cancellationToken)
        where TEntity : class
    {
        if (entidades.Count == 0)
        {
            return;
        }

        // "tabla" solo puede ser una de las llamadas fijas de ImportarAsync (nunca
        // entrada de usuario) — se valida contra la lista de tablas conocida antes
        // de interpolarla en SQL, ya que SQL Server no permite parametrizar el
        // nombre de una tabla.
        if (!TablasEnOrdenDeBorrado.Contains(tabla))
        {
            throw new ArgumentOutOfRangeException(nameof(tabla), tabla, "Tabla no reconocida para IDENTITY_INSERT.");
        }

#pragma warning disable EF1002
        await context.Database.ExecuteSqlRawAsync($"SET IDENTITY_INSERT [{tabla}] ON", cancellationToken);
        context.Set<TEntity>().AddRange(entidades);
        await context.SaveChangesAsync(cancellationToken);
        await context.Database.ExecuteSqlRawAsync($"SET IDENTITY_INSERT [{tabla}] OFF", cancellationToken);
#pragma warning restore EF1002
    }

    private static Sede MapearSede(SedeExport s) => new() { Id = s.Id, Nombre = s.Nombre, Municipio = s.Municipio, Direccion = s.Direccion };

    private static Club MapearClub(ClubExport c) => new() { Id = c.Id, Nombre = c.Nombre, Municipio = c.Municipio, FechaAlta = c.FechaAlta };

    private static Temporada MapearTemporada(TemporadaExport t) =>
        new() { Id = t.Id, Nombre = t.Nombre, FechaInicio = t.FechaInicio, FechaFin = t.FechaFin, Estado = t.Estado };

    private static Categoria MapearCategoria(CategoriaExport c) => new() { Id = c.Id, Nombre = c.Nombre, Orden = c.Orden };

    private static Competicion MapearCompeticion(CompeticionExport c) => new()
    {
        Id = c.Id, TemporadaId = c.TemporadaId, CategoriaId = c.CategoriaId,
        PuntosVictoria = c.PuntosVictoria, PuntosDerrota = c.PuntosDerrota,
    };

    private static Equipo MapearEquipo(EquipoExport e) => new()
    {
        Id = e.Id, CompeticionId = e.CompeticionId, ClubId = e.ClubId, Nombre = e.Nombre,
        SedeHabitualId = e.SedeHabitualId, Estado = e.Estado,
    };

    private static FichaJugador MapearFichaJugador(FichaJugadorExport f) =>
        new() { Id = f.Id, EquipoId = f.EquipoId, Dorsal = f.Dorsal, Posicion = f.Posicion };

    private static Jornada MapearJornada(JornadaExport j) => new()
    {
        Id = j.Id, CompeticionId = j.CompeticionId, Numero = j.Numero,
        Etiqueta = j.Etiqueta, CuentaParaClasificacion = j.CuentaParaClasificacion,
    };

    private static Partido MapearPartido(PartidoExport p) => new()
    {
        Id = p.Id, JornadaId = p.JornadaId, EquipoLocalId = p.EquipoLocalId, EquipoVisitanteId = p.EquipoVisitanteId,
        SedeId = p.SedeId, FechaHora = p.FechaHora, Estado = p.Estado, PuntosLocal = p.PuntosLocal,
        PuntosVisitante = p.PuntosVisitante, MotivoResolucion = p.MotivoResolucion,
        EquipoGanadorResolucionId = p.EquipoGanadorResolucionId, Observaciones = p.Observaciones,
    };

    private static PartidoParcial MapearPartidoParcial(PartidoParcialExport pp) => new()
    {
        Id = pp.Id, PartidoId = pp.PartidoId, NumeroPeriodo = pp.NumeroPeriodo,
        PuntosLocal = pp.PuntosLocal, PuntosVisitante = pp.PuntosVisitante,
    };

    private static PenalizacionClasificacion MapearPenalizacion(PenalizacionClasificacionExport p) => new()
    {
        Id = p.Id, EquipoId = p.EquipoId, Puntos = p.Puntos, Motivo = p.Motivo,
        PartidoId = p.PartidoId, FechaAplicacion = p.FechaAplicacion,
    };
}
