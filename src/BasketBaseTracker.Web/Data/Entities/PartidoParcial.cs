using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace BasketBaseTracker.Web.Data.Entities;

// Tabla hija en vez de columnas fijas Q1-Q4 para no forzar el número de
// prórrogas en el esquema (data-model.md).
public class PartidoParcial
{
    public int Id { get; set; }

    public int PartidoId { get; set; }

    [ValidateNever]
    public Partido Partido { get; set; } = null!;

    // 1-4; 5+ para prórrogas.
    [Display(Name = "Periodo")]
    public int NumeroPeriodo { get; set; }

    [Display(Name = "Puntos local")]
    public int PuntosLocal { get; set; }

    [Display(Name = "Puntos visitante")]
    public int PuntosVisitante { get; set; }
}
