using System.ComponentModel.DataAnnotations;

namespace BasketBaseTracker.Web.Data.Entities;

public class Club
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El campo {0} es obligatorio.")]
    [StringLength(200, ErrorMessage = "El campo {0} no puede superar los {1} caracteres.")]
    public string Nombre { get; set; } = null!;

    [Required(ErrorMessage = "El campo {0} es obligatorio.")]
    [StringLength(200, ErrorMessage = "El campo {0} no puede superar los {1} caracteres.")]
    public string Municipio { get; set; } = null!;

    [Display(Name = "Fecha de alta")]
    public DateOnly FechaAlta { get; set; }
}
