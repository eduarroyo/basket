namespace BasketBaseTracker.Web.Data.Entities;

public class Club
{
    public int Id { get; set; }

    public string Nombre { get; set; } = null!;

    public string Municipio { get; set; } = null!;

    public DateOnly FechaAlta { get; set; }
}
