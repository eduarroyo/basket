using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace BasketBaseTracker.Web.Data.Entities;

// Etiqueta y CuentaParaClasificacion permiten representar fases finales (copa,
// playoff) sin modelar un cuadro/bracket — architecture.md punto 16.
public class Jornada
{
    public int Id { get; set; }

    public int CompeticionId { get; set; }

    [ValidateNever]
    public Competicion Competicion { get; set; } = null!;

    public int Numero { get; set; }

    [StringLength(200, ErrorMessage = "El campo {0} no puede superar los {1} caracteres.")]
    public string? Etiqueta { get; set; }

    [Display(Name = "Cuenta para la clasificación")]
    public bool CuentaParaClasificacion { get; set; } = true;
}
