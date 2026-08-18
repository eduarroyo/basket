namespace BasketBaseTracker.Web.Data.Entities;

// Tabla hija en vez de columnas fijas Q1-Q4 para no forzar el número de
// prórrogas en el esquema (data-model.md).
public class PartidoParcial
{
    public int Id { get; set; }

    public int PartidoId { get; set; }

    public Partido Partido { get; set; } = null!;

    // 1-4; 5+ para prórrogas.
    public int NumeroPeriodo { get; set; }

    public int PuntosLocal { get; set; }

    public int PuntosVisitante { get; set; }
}
