using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace BasketBaseTracker.Web.Data.Entities;

public class Competicion
{
    public int Id { get; set; }

    public int TemporadaId { get; set; }

    // [ValidateNever]: sin esto, al no ser nullable, ASP.NET Core la trata como
    // implícitamente obligatoria (nullable reference types) — el formulario de
    // Admin nunca la rellena (solo envía TemporadaId), así que ModelState.IsValid
    // sería siempre falso sin que ningún mensaje visible lo explique
    // (asp-validation-summary="ModelOnly" no muestra errores de propiedad).
    [ValidateNever]
    public Temporada Temporada { get; set; } = null!;

    public int CategoriaId { get; set; }

    [ValidateNever]
    public Categoria Categoria { get; set; } = null!;

    // Valores por defecto del Reglamento General y de Competiciones de la F.A.B.
    // (Art. 77) para el sistema de liga — configurables por competición, no fijos
    // en el esquema (data-model.md).
    [Display(Name = "Puntos por victoria")]
    public int PuntosVictoria { get; set; } = 2;

    [Display(Name = "Puntos por derrota")]
    public int PuntosDerrota { get; set; } = 1;
}
