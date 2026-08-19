using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace BasketBaseTracker.Web.Data.Entities;

public enum EquipoEstado
{
    Activo,
    Retirado,
}

// Representa la participación de un club en una competición concreta, no una
// entidad persistente entre temporadas (data-model.md).
public class Equipo
{
    public int Id { get; set; }

    public int CompeticionId { get; set; }

    // Ver el comentario equivalente en Competicion.cs — sin [ValidateNever], la
    // validación implícita de nullable reference types rechaza el alta/edición en
    // silencio porque el formulario solo envía CompeticionId/ClubId, no estos
    // objetos completos.
    [ValidateNever]
    public Competicion Competicion { get; set; } = null!;

    public int ClubId { get; set; }

    [ValidateNever]
    public Club Club { get; set; } = null!;

    [Required(ErrorMessage = "El campo {0} es obligatorio.")]
    [StringLength(200, ErrorMessage = "El campo {0} no puede superar los {1} caracteres.")]
    public string Nombre { get; set; } = null!;

    [Display(Name = "Sede habitual")]
    public int? SedeHabitualId { get; set; }

    public Sede? SedeHabitual { get; set; }

    public EquipoEstado Estado { get; set; }

    // Concurrencia optimista (data-model.md) — evita ediciones concurrentes
    // perdidas entre administradores.
    public byte[]? RowVersion { get; set; }
}
