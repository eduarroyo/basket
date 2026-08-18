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

    public Competicion Competicion { get; set; } = null!;

    public int ClubId { get; set; }

    public Club Club { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    public int? SedeHabitualId { get; set; }

    public Sede? SedeHabitual { get; set; }

    public EquipoEstado Estado { get; set; }

    // Concurrencia optimista (data-model.md) — evita ediciones concurrentes
    // perdidas entre administradores.
    public byte[]? RowVersion { get; set; }
}
