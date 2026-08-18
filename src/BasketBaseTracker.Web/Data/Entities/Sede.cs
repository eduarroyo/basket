using System.ComponentModel.DataAnnotations;

namespace BasketBaseTracker.Web.Data.Entities;

// El pabellón físico no cambia de una temporada a otra; qué equipo lo usa como
// sede habitual se modela en Equipo, no aquí (data-model.md).
public class Sede
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El campo {0} es obligatorio.")]
    [StringLength(200, ErrorMessage = "El campo {0} no puede superar los {1} caracteres.")]
    public string Nombre { get; set; } = null!;

    [Required(ErrorMessage = "El campo {0} es obligatorio.")]
    [StringLength(200, ErrorMessage = "El campo {0} no puede superar los {1} caracteres.")]
    public string Municipio { get; set; } = null!;

    [Required(ErrorMessage = "El campo {0} es obligatorio.")]
    [StringLength(500, ErrorMessage = "El campo {0} no puede superar los {1} caracteres.")]
    public string Direccion { get; set; } = null!;
}
