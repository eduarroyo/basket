using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;

namespace BasketBaseTracker.Web.Domain;

// Duración fija por falta de hora de fin en el modelo de datos — ver
// spec.md de BAS-14, Aclaraciones.
public static class IcsFeedBuilder
{
    private static readonly TimeSpan DuracionPartido = TimeSpan.FromHours(2);
    private const string ZonaHoraria = "Europe/Madrid";

    public static string Construir(IEnumerable<PartidoIcs> partidos)
    {
        var calendar = new Calendar();

        foreach (var partido in partidos)
        {
            var evento = new CalendarEvent
            {
                Uid = $"partido-{partido.Id}@basketbasetracker.es",
                Summary = $"{partido.EquipoLocal} - {partido.EquipoVisitante}",
                Start = new CalDateTime(partido.FechaHora, ZonaHoraria),
                Duration = Duration.FromTimeSpanExact(DuracionPartido),
            };

            if (partido.SedeNombre is not null)
            {
                evento.Location = partido.SedeMunicipio is null
                    ? partido.SedeNombre
                    : $"{partido.SedeNombre}, {partido.SedeMunicipio}";
            }

            calendar.Events.Add(evento);
        }

        return new CalendarSerializer().SerializeToString(calendar) ?? string.Empty;
    }
}

public readonly record struct PartidoIcs(
    int Id,
    string EquipoLocal,
    string EquipoVisitante,
    DateTime FechaHora,
    string? SedeNombre,
    string? SedeMunicipio);
