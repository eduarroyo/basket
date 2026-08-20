namespace BasketBaseTracker.Seed.Dominio;

public sealed record ClubGenerado(string Nombre, string Municipio);

// Datos de catálogo estático (municipios reales de la provincia de Sevilla,
// categorías estándar del baloncesto base) usados para generar un dataset de
// demostración plausible — plan.md de BAS-17, decisión técnica 2. Sin
// librería de datos falsos (Bogus/Faker): el volumen es pequeño y no
// justifica una dependencia nueva.
public static class Catalogo
{
    public static readonly IReadOnlyList<string> Municipios =
    [
        "Sevilla", "Dos Hermanas", "Alcalá de Guadaíra", "Utrera", "Écija",
        "Mairena del Aljarafe", "Los Palacios y Villafranca", "La Rinconada",
        "Coria del Río", "San Juan de Aznalfarache", "Camas", "Bormujos",
        "Tomares", "Carmona", "Morón de la Frontera", "Lebrija", "Marchena",
        "Osuna", "Sanlúcar la Mayor", "Espartinas", "Gines", "Pilas",
        "Herrera", "La Puebla de Cazalla",
    ];

    // Orden ascendente por edad, de menor a mayor (Orden de Categoria, usado
    // p. ej. para ordenar la portada pública — screens.md).
    public static readonly IReadOnlyList<(string Nombre, int Orden)> Categorias =
    [
        ("Benjamín", 1),
        ("Alevín", 2),
        ("Infantil", 3),
        ("Cadete", 4),
        ("Juvenil", 5),
    ];

    public static IReadOnlyList<ClubGenerado> GenerarClubes(int numeroDeClubes)
    {
        var clubes = new List<ClubGenerado>();
        for (var i = 0; i < numeroDeClubes; i++)
        {
            var municipio = Municipios[i % Municipios.Count];
            // A partir de la segunda vuelta por la lista de municipios (más clubes
            // que municipios catalogados), añade un sufijo para no repetir nombre.
            var vuelta = i / Municipios.Count;
            var nombre = vuelta == 0 ? $"CB {municipio}" : $"CB {municipio} {vuelta + 1}";
            clubes.Add(new ClubGenerado(nombre, municipio));
        }

        return clubes;
    }
}
