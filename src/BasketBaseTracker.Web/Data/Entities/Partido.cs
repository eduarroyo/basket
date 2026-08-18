namespace BasketBaseTracker.Web.Data.Entities;

public enum PartidoEstado
{
    Programado,
    Jugado,
    Aplazado,
    Cancelado,
    Resuelto,
}

public enum MotivoResolucion
{
    Incomparecencia,
    AlineacionIndebida,
    Otro,
}

public class Partido
{
    public int Id { get; set; }

    public int JornadaId { get; set; }

    public Jornada Jornada { get; set; } = null!;

    public int EquipoLocalId { get; set; }

    public Equipo EquipoLocal { get; set; } = null!;

    public int EquipoVisitanteId { get; set; }

    public Equipo EquipoVisitante { get; set; } = null!;

    // Por defecto la sede habitual del local; permite excepciones (finales,
    // partidos reubicados) — data-model.md.
    public int? SedeId { get; set; }

    public Sede? Sede { get; set; }

    public DateTime? FechaHora { get; set; }

    public PartidoEstado Estado { get; set; }

    public int? PuntosLocal { get; set; }

    public int? PuntosVisitante { get; set; }

    // Solo si Estado = Resuelto (data-model.md).
    public MotivoResolucion? MotivoResolucion { get; set; }

    public int? EquipoGanadorResolucionId { get; set; }

    public Equipo? EquipoGanadorResolucion { get; set; }

    public string? Observaciones { get; set; }

    // Concurrencia optimista (data-model.md) — evita ediciones concurrentes
    // perdidas entre administradores.
    public byte[]? RowVersion { get; set; }
}
