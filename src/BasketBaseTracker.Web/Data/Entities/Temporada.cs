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

    public string Nombre { get; set; } = null!;

    public DateOnly FechaInicio { get; set; }

    public DateOnly FechaFin { get; set; }

    public TemporadaEstado Estado { get; set; }
}
