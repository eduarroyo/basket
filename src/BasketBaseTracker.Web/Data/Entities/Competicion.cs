namespace BasketBaseTracker.Web.Data.Entities;

public class Competicion
{
    public int Id { get; set; }

    public int TemporadaId { get; set; }

    public Temporada Temporada { get; set; } = null!;

    public int CategoriaId { get; set; }

    public Categoria Categoria { get; set; } = null!;

    // Valores por defecto del Reglamento General y de Competiciones de la F.A.B.
    // (Art. 77) para el sistema de liga — configurables por competición, no fijos
    // en el esquema (data-model.md).
    public int PuntosVictoria { get; set; } = 2;

    public int PuntosDerrota { get; set; } = 1;
}
