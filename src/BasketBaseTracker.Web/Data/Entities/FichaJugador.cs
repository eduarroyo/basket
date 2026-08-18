namespace BasketBaseTracker.Web.Data.Entities;

public enum Posicion
{
    Base,
    Escolta,
    Alero,
    AlaPivot,
    Pivot,
}

// Sustituye a un "Jugador" como entidad fuerte: deliberadamente sin nombre ni
// ningún dato identificativo, por la decisión de no tratar datos personales
// (RGPD, data-model.md).
public class FichaJugador
{
    public int Id { get; set; }

    public int EquipoId { get; set; }

    public Equipo Equipo { get; set; } = null!;

    public int Dorsal { get; set; }

    public Posicion? Posicion { get; set; }
}
