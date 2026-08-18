namespace BasketBaseTracker.Web.Data.Entities;

// Registro manual de una penalización de puntos de clasificación por
// expediente disciplinario (Art. 43 del Reglamento Disciplinario de la
// F.A.B.) — ajuste independiente del resultado de cualquier partido concreto
// (data-model.md). No lleva CompeticionId propio: se obtiene vía EquipoId.
public class PenalizacionClasificacion
{
    public int Id { get; set; }

    public int EquipoId { get; set; }

    public Equipo Equipo { get; set; } = null!;

    // Negativo para un descuento, p. ej. -1.
    public int Puntos { get; set; }

    public string Motivo { get; set; } = null!;

    public int? PartidoId { get; set; }

    public Partido? Partido { get; set; }

    public DateOnly FechaAplicacion { get; set; }
}
