using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

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

    [ValidateNever]
    public Jornada Jornada { get; set; } = null!;

    [Display(Name = "Equipo local")]
    public int EquipoLocalId { get; set; }

    [ValidateNever]
    public Equipo EquipoLocal { get; set; } = null!;

    [Display(Name = "Equipo visitante")]
    public int EquipoVisitanteId { get; set; }

    [ValidateNever]
    public Equipo EquipoVisitante { get; set; } = null!;

    // Por defecto la sede habitual del local; permite excepciones (finales,
    // partidos reubicados) — data-model.md.
    [Display(Name = "Sede")]
    public int? SedeId { get; set; }

    [ValidateNever]
    public Sede? Sede { get; set; }

    [Display(Name = "Fecha y hora")]
    public DateTime? FechaHora { get; set; }

    public PartidoEstado Estado { get; set; }

    public int? PuntosLocal { get; set; }

    public int? PuntosVisitante { get; set; }

    // Solo si Estado = Resuelto (data-model.md).
    public MotivoResolucion? MotivoResolucion { get; set; }

    public int? EquipoGanadorResolucionId { get; set; }

    [ValidateNever]
    public Equipo? EquipoGanadorResolucion { get; set; }

    public string? Observaciones { get; set; }

    // Concurrencia optimista (data-model.md) — evita ediciones concurrentes
    // perdidas entre administradores.
    public byte[]? RowVersion { get; set; }
}
