using System.ComponentModel.DataAnnotations;

namespace BasketBaseTracker.Web.Data.Entities;

public class Categoria
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El campo {0} es obligatorio.")]
    [StringLength(100, ErrorMessage = "El campo {0} no puede superar los {1} caracteres.")]
    public string Nombre { get; set; } = null!;

    public int Orden { get; set; }
}
