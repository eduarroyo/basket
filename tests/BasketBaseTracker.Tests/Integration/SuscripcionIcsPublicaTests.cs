using System.Net;
using static BasketBaseTracker.Tests.Integration.AdminHttpTestHelpers;

namespace BasketBaseTracker.Tests.Integration;

// Cubre BAS-14: feeds .ics por competición y por equipo, filtrado de partidos
// sin fecha o cancelados, 404 en id inexistente y enlace de suscripción visible
// en las páginas públicas ya cubiertas por BAS-13.
public class SuscripcionIcsPublicaTests(AppHostSqlFixture fixture) : IClassFixture<AppHostSqlFixture>
{
    private static async Task<string> CrearSedeAsync(HttpClient client, string nombre, CancellationToken cancellationToken)
    {
        var createPage = await client.GetAsync("/Admin/Sede/Create", cancellationToken);
        var token = await GetAntiforgeryTokenAsync(createPage, cancellationToken);

        var response = await client.PostAsync(
            "/Admin/Sede/Create",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["Sede.Nombre"] = nombre,
                ["Sede.Municipio"] = "Sevilla",
                ["Sede.Direccion"] = "Calle de Prueba, 1",
            }),
            cancellationToken);
        var html = await response.Content.ReadAsStringAsync(cancellationToken);

        return ExtraerId(html, "Sede", nombre);
    }

    private static async Task AsignarFechaYSedeAsync(
        HttpClient client, string partidoId, string equipoLocalId, string equipoVisitanteId, string sedeId, CancellationToken cancellationToken)
    {
        var editUrl = $"/Admin/Partido/Edit/{partidoId}";
        var editPage = await client.GetAsync(editUrl, cancellationToken);
        var token = await GetAntiforgeryTokenAsync(editPage, cancellationToken);

        var response = await client.PostAsync(
            editUrl,
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["Partido.Id"] = partidoId,
                ["Partido.EquipoLocalId"] = equipoLocalId,
                ["Partido.EquipoVisitanteId"] = equipoVisitanteId,
                ["Partido.SedeId"] = sedeId,
                ["Partido.FechaHora"] = "2026-04-10T18:00",
            }),
            cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private static async Task CancelarPartidoAsync(HttpClient client, string partidoId, CancellationToken cancellationToken)
    {
        var editUrl = $"/Admin/Resultado/Edit/{partidoId}";
        var editPage = await client.GetAsync(editUrl, cancellationToken);
        var editHtml = await editPage.Content.ReadAsStringAsync(cancellationToken);
        var token = await GetAntiforgeryTokenAsync(editPage, cancellationToken);
        var rowVersion = ExtraerCampoOculto(editHtml, "Partido.RowVersion");

        var response = await client.PostAsync(
            editUrl,
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["Partido.Id"] = partidoId,
                ["Partido.RowVersion"] = rowVersion,
                ["Partido.Estado"] = "3", // Cancelado
            }),
            cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private static int ContarVEvents(string ics) => ics.Split("BEGIN:VEVENT").Length - 1;

    [Fact]
    public async Task FeedDeCompeticionIncluyeSoloPartidosConFechaYNoCanceladosYRechaza404()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = fixture.CreateWebHttpClient();
        await LoginAsync(client, cancellationToken);

        var (competicionId, equipoIds, equipoNombres, _, _) = await CrearCompeticionConEquiposAsync(client, "ics-competicion", numeroDeEquipos: 4, cancellationToken);
        var jornada1 = await CrearJornadaAsync(client, competicionId, numero: 1, etiqueta: "Jornada 1 (ics-competicion)", cancellationToken);
        var jornada2 = await CrearJornadaAsync(client, competicionId, numero: 2, etiqueta: "Jornada 2 (ics-competicion)", cancellationToken);
        var sedeId = await CrearSedeAsync(client, "Pabellon ICS (ics-competicion)", cancellationToken);

        // Incluido: fecha y sede asignadas.
        var partidoIncluido = await CrearPartidoAsync(client, jornada1, equipoIds[0], equipoIds[1], equipoNombres[0], cancellationToken);
        await AsignarFechaYSedeAsync(client, partidoIncluido, equipoIds[0], equipoIds[1], sedeId, cancellationToken);

        // Excluido: sin fecha (queda "Sin programar", como al crearlo).
        await CrearPartidoAsync(client, jornada1, equipoIds[2], equipoIds[3], equipoNombres[2], cancellationToken);

        // Excluido: tenía fecha pero se cancela.
        var partidoCancelado = await CrearPartidoAsync(client, jornada2, equipoIds[0], equipoIds[2], equipoNombres[0], cancellationToken);
        await AsignarFechaYSedeAsync(client, partidoCancelado, equipoIds[0], equipoIds[2], sedeId, cancellationToken);
        await CancelarPartidoAsync(client, partidoCancelado, cancellationToken);

        using var anonimo = fixture.CreateAnonymousWebHttpClient();
        using var respuesta = await anonimo.GetAsync($"/competiciones/{competicionId}/calendario.ics", cancellationToken);
        var ics = await respuesta.Content.ReadAsStringAsync(cancellationToken);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal("text/calendar", respuesta.Content.Headers.ContentType?.MediaType);
        Assert.Equal("utf-8", respuesta.Content.Headers.ContentType?.CharSet);
        Assert.Equal(1, ContarVEvents(ics));

        var evento = Ical.Net.Calendar.Load(ics)!.Events.Single();
        Assert.Equal($"partido-{partidoIncluido}@basketbasetracker.es", evento.Uid);
        Assert.Equal($"{equipoNombres[0]} - {equipoNombres[1]}", evento.Summary);
        Assert.Equal("Pabellon ICS (ics-competicion), Sevilla", evento.Location);

        using var respuesta404 = await anonimo.GetAsync("/competiciones/999999/calendario.ics", cancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, respuesta404.StatusCode);
    }

    [Fact]
    public async Task FeedDeEquipoIncluyePartidosComoLocalYVisitanteYRechaza404()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = fixture.CreateWebHttpClient();
        await LoginAsync(client, cancellationToken);

        var (competicionId, equipoIds, equipoNombres, _, _) = await CrearCompeticionConEquiposAsync(client, "ics-equipo", numeroDeEquipos: 3, cancellationToken);
        var jornada1 = await CrearJornadaAsync(client, competicionId, numero: 1, etiqueta: "Jornada 1 (ics-equipo)", cancellationToken);
        var jornada2 = await CrearJornadaAsync(client, competicionId, numero: 2, etiqueta: "Jornada 2 (ics-equipo)", cancellationToken);
        var sedeId = await CrearSedeAsync(client, "Pabellon ICS Equipo (ics-equipo)", cancellationToken);

        // El equipo 0 juega como local en la jornada 1 y como visitante en la 2.
        var partidoLocal = await CrearPartidoAsync(client, jornada1, equipoIds[0], equipoIds[1], equipoNombres[0], cancellationToken);
        await AsignarFechaYSedeAsync(client, partidoLocal, equipoIds[0], equipoIds[1], sedeId, cancellationToken);

        var partidoVisitante = await CrearPartidoAsync(client, jornada2, equipoIds[2], equipoIds[0], equipoNombres[2], cancellationToken);
        await AsignarFechaYSedeAsync(client, partidoVisitante, equipoIds[2], equipoIds[0], sedeId, cancellationToken);

        using var anonimo = fixture.CreateAnonymousWebHttpClient();
        using var respuesta = await anonimo.GetAsync($"/equipos/{equipoIds[0]}/calendario.ics", cancellationToken);
        var ics = await respuesta.Content.ReadAsStringAsync(cancellationToken);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal("text/calendar", respuesta.Content.Headers.ContentType?.MediaType);
        Assert.Equal(2, ContarVEvents(ics));
        Assert.Contains($"UID:partido-{partidoLocal}@basketbasetracker.es", ics);
        Assert.Contains($"UID:partido-{partidoVisitante}@basketbasetracker.es", ics);

        using var respuesta404 = await anonimo.GetAsync("/equipos/999999/calendario.ics", cancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, respuesta404.StatusCode);
    }

    [Fact]
    public async Task LasPaginasPublicasMuestranElEnlaceDeSuscripcion()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = fixture.CreateWebHttpClient();
        await LoginAsync(client, cancellationToken);

        var (competicionId, equipoIds, _, _, _) = await CrearCompeticionConEquiposAsync(client, "ics-enlace", numeroDeEquipos: 1, cancellationToken);

        using var anonimo = fixture.CreateAnonymousWebHttpClient();
        var htmlCalendario = await (await anonimo.GetAsync($"/competiciones/{competicionId}/calendario", cancellationToken)).Content.ReadAsStringAsync(cancellationToken);
        var htmlEquipo = await (await anonimo.GetAsync($"/equipos/{equipoIds[0]}", cancellationToken)).Content.ReadAsStringAsync(cancellationToken);

        Assert.Contains($"href=\"/competiciones/{competicionId}/calendario.ics\"", htmlCalendario);
        Assert.Contains($"href=\"/equipos/{equipoIds[0]}/calendario.ics\"", htmlEquipo);
    }

    [Fact]
    public async Task LosFeedsIcsLlevanOutputCachingActivo()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = fixture.CreateWebHttpClient();
        await LoginAsync(client, cancellationToken);

        var (competicionId, equipoIds, _, _, _) = await CrearCompeticionConEquiposAsync(client, "ics-cache", numeroDeEquipos: 1, cancellationToken);

        using var anonimo = fixture.CreateAnonymousWebHttpClient();
        using var primeraCompeticion = await anonimo.GetAsync($"/competiciones/{competicionId}/calendario.ics", cancellationToken);
        using var segundaCompeticion = await anonimo.GetAsync($"/competiciones/{competicionId}/calendario.ics", cancellationToken);
        using var primeraEquipo = await anonimo.GetAsync($"/equipos/{equipoIds[0]}/calendario.ics", cancellationToken);
        using var segundaEquipo = await anonimo.GetAsync($"/equipos/{equipoIds[0]}/calendario.ics", cancellationToken);

        Assert.False(primeraCompeticion.Headers.Contains("Age"));
        Assert.True(segundaCompeticion.Headers.Contains("Age"));
        Assert.False(primeraEquipo.Headers.Contains("Age"));
        Assert.True(segundaEquipo.Headers.Contains("Age"));
    }
}
