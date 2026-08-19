using BasketBaseTracker.Web.Domain;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;

namespace BasketBaseTracker.Tests.Unit;

public class IcsFeedBuilderTests
{
    private static readonly DateTime FechaHora = new(2026, 10, 4, 12, 0, 0, DateTimeKind.Unspecified);

    [Fact]
    public void GeneraUnEventoConLosCamposEsperadosParaUnPartidoConSede()
    {
        PartidoIcs[] partidos =
        [
            new(Id: 1, EquipoLocal: "CB Triana A", EquipoVisitante: "CB Nervión", FechaHora, SedeNombre: "Pabellón San Pablo", SedeMunicipio: "Sevilla"),
        ];

        var ics = IcsFeedBuilder.Construir(partidos);
        var evento = Calendar.Load(ics)!.Events.Single();

        Assert.Equal("partido-1@basketbasetracker.es", evento.Uid);
        Assert.Equal("CB Triana A - CB Nervión", evento.Summary);
        Assert.Equal(FechaHora, evento.Start!.Value);
        Assert.Equal(Duration.FromTimeSpanExact(TimeSpan.FromHours(2)), evento.Duration);
        Assert.Equal("Pabellón San Pablo, Sevilla", evento.Location);
    }

    [Fact]
    public void OmiteLaUbicacionCuandoElPartidoNoTieneSede()
    {
        PartidoIcs[] partidos =
        [
            new(Id: 2, EquipoLocal: "CB Triana A", EquipoVisitante: "CB Nervión", FechaHora, SedeNombre: null, SedeMunicipio: null),
        ];

        var ics = IcsFeedBuilder.Construir(partidos);
        var evento = Calendar.Load(ics)!.Events.Single();

        Assert.True(string.IsNullOrEmpty(evento.Location));
    }

    [Fact]
    public void ElUidEsEstableEntreDosGeneracionesDelMismoPartido()
    {
        PartidoIcs[] partidos =
        [
            new(Id: 42, EquipoLocal: "CB Triana A", EquipoVisitante: "CB Nervión", FechaHora, SedeNombre: null, SedeMunicipio: null),
        ];

        var primeraGeneracion = ((CalendarEvent)Calendar.Load(IcsFeedBuilder.Construir(partidos))!.Events.Single()).Uid;
        var segundaGeneracion = ((CalendarEvent)Calendar.Load(IcsFeedBuilder.Construir(partidos))!.Events.Single()).Uid;

        Assert.Equal(primeraGeneracion, segundaGeneracion);
    }
}
