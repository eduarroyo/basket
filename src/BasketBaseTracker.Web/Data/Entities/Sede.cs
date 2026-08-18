namespace BasketBaseTracker.Web.Data.Entities;

// El pabellón físico no cambia de una temporada a otra; qué equipo lo usa como
// sede habitual se modela en Equipo, no aquí (data-model.md).
public class Sede
{
    public int Id { get; set; }

    public string Nombre { get; set; } = null!;

    public string Municipio { get; set; } = null!;

    public string Direccion { get; set; } = null!;
}
