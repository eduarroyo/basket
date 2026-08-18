namespace BasketBaseTracker.Web.Data.Entities;

public class Categoria
{
    public int Id { get; set; }

    public string Nombre { get; set; } = null!;

    public int Orden { get; set; }
}
