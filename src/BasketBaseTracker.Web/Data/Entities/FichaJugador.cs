using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace BasketBaseTracker.Web.Data.Entities;

public enum Posicion
{
    Base,
    Escolta,
    Alero,
    [Display(Name = "Ala-Pívot")]
    AlaPivot,
    [Display(Name = "Pívot")]
    Pivot,
}

// Sustituye a un "Jugador" como entidad fuerte: deliberadamente sin nombre ni
// ningún dato identificativo, por la decisión de no tratar datos personales
// (RGPD, data-model.md).
public class FichaJugador
{
    public int Id { get; set; }

    public int EquipoId { get; set; }

    // [ValidateNever]: sin esto, ASP.NET Core la trata como implícitamente
    // obligatoria (nullable reference types) y el alta/edición falla en silencio —
    // ver BAS-7, plan.md, para el detalle completo del problema.
    [ValidateNever]
    public Equipo Equipo { get; set; } = null!;

    public int Dorsal { get; set; }

    public Posicion? Posicion { get; set; }
}
