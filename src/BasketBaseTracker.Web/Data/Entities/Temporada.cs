using System.ComponentModel.DataAnnotations;

namespace BasketBaseTracker.Web.Data.Entities;

public enum TemporadaEstado
{
    Planificada,
    EnCurso,
    Finalizada,
    Archivada,
}

public class Temporada
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El campo {0} es obligatorio.")]
    [StringLength(50, ErrorMessage = "El campo {0} no puede superar los {1} caracteres.")]
    public string Nombre { get; set; } = null!;

    [Display(Name = "Fecha de inicio")]
    public DateOnly FechaInicio { get; set; }

    [Display(Name = "Fecha de fin")]
    public DateOnly FechaFin { get; set; }

    [Required(ErrorMessage = "El campo {0} es obligatorio.")]
    public TemporadaEstado Estado { get; set; }
}
