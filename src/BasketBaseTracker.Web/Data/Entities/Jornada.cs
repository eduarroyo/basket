namespace BasketBaseTracker.Web.Data.Entities;

// Etiqueta y CuentaParaClasificacion permiten representar fases finales (copa,
// playoff) sin modelar un cuadro/bracket — architecture.md punto 16.
public class Jornada
{
    public int Id { get; set; }

    public int CompeticionId { get; set; }

    public Competicion Competicion { get; set; } = null!;

    public int Numero { get; set; }

    public string? Etiqueta { get; set; }

    public bool CuentaParaClasificacion { get; set; } = true;
}
