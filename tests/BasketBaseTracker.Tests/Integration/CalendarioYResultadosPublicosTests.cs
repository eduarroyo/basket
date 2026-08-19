using static BasketBaseTracker.Tests.Integration.AdminHttpTestHelpers;

namespace BasketBaseTracker.Tests.Integration;

// Cubre el resto del calendario/resultados públicos de BAS-13 (Calendario de
// competición, Detalle de partido, Resultados por jornada, Resultados por
// equipo) vía HTTP real contra el AppHost completo: contenido, enlaces de
// navegación, 404 en id/n inexistente y Output Caching activo (misma técnica
// de verificación que BAS-11/BAS-12).
public class CalendarioYResultadosPublicosTests(AppHostSqlFixture fixture) : IClassFixture<AppHostSqlFixture>
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

    private static async Task AsignarSedeAPartidoAsync(
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

    private static async Task CrearParcialAsync(HttpClient client, string partidoId, int periodo, int puntosLocal, int puntosVisitante, CancellationToken cancellationToken)
    {
        var createPage = await client.GetAsync($"/Admin/PartidoParcial/Create/{partidoId}", cancellationToken);
        var token = await GetAntiforgeryTokenAsync(createPage, cancellationToken);

        var response = await client.PostAsync(
            $"/Admin/PartidoParcial/Create/{partidoId}",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["PartidoParcial.NumeroPeriodo"] = periodo.ToString(),
                ["PartidoParcial.PuntosLocal"] = puntosLocal.ToString(),
                ["PartidoParcial.PuntosVisitante"] = puntosVisitante.ToString(),
            }),
            cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task CalendarioMuestraJornadasYPartidosConEnlaces()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = fixture.CreateWebHttpClient();
        await LoginAsync(client, cancellationToken);

        var (competicionId, equipoIds, equipoNombres, _, _) = await CrearCompeticionConEquiposAsync(client, "calendario-publico", numeroDeEquipos: 2, cancellationToken);
        var jornadaId = await CrearJornadaAsync(client, competicionId, numero: 1, etiqueta: "Jornada 1 (calendario-publico)", cancellationToken);
        var partidoId = await CrearPartidoAsync(client, jornadaId, equipoIds[0], equipoIds[1], equipoNombres[0], cancellationToken);

        using var anonimo = fixture.CreateAnonymousWebHttpClient();
        var html = await (await anonimo.GetAsync($"/competiciones/{competicionId}/calendario", cancellationToken)).Content.ReadAsStringAsync(cancellationToken);

        Assert.Contains("Jornada 1 (calendario-publico)", html);
        Assert.Contains(equipoNombres[0], html);
        Assert.Contains(equipoNombres[1], html);
        Assert.Contains("Sin programar", html);
        Assert.Contains($"/competiciones/{competicionId}/jornadas/1", html);
        Assert.Contains($"/partidos/{partidoId}", html);
    }

    [Fact]
    public async Task ResultadosPorJornadaMuestraMarcadorOEstadoYRechaza404EnNumeroInexistente()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = fixture.CreateWebHttpClient();
        await LoginAsync(client, cancellationToken);

        var (competicionId, equipoIds, equipoNombres, _, _) = await CrearCompeticionConEquiposAsync(client, "jornada-publica", numeroDeEquipos: 2, cancellationToken);
        var jornadaId = await CrearJornadaAsync(client, competicionId, numero: 1, etiqueta: "Jornada 1 (jornada-publica)", cancellationToken);
        var partidoId = await CrearPartidoAsync(client, jornadaId, equipoIds[0], equipoIds[1], equipoNombres[0], cancellationToken);
        await MarcarPartidoJugadoAsync(client, partidoId, puntosLocal: 85, puntosVisitante: 80, cancellationToken);

        using var anonimo = fixture.CreateAnonymousWebHttpClient();
        var html = await (await anonimo.GetAsync($"/competiciones/{competicionId}/jornadas/1", cancellationToken)).Content.ReadAsStringAsync(cancellationToken);

        Assert.Contains(equipoNombres[0], html);
        Assert.Contains(equipoNombres[1], html);
        Assert.Contains("85-80", html);

        using var respuesta404 = await anonimo.GetAsync($"/competiciones/{competicionId}/jornadas/999999", cancellationToken);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, respuesta404.StatusCode);
    }

    [Fact]
    public async Task DetalleDePartidoMuestraMarcadorParcialesSedeYResolucionAdministrativa()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = fixture.CreateWebHttpClient();
        await LoginAsync(client, cancellationToken);

        var (competicionId, equipoIds, equipoNombres, _, _) = await CrearCompeticionConEquiposAsync(client, "detalle-partido", numeroDeEquipos: 2, cancellationToken);
        var jornadaId = await CrearJornadaAsync(client, competicionId, numero: 1, etiqueta: "Jornada 1 (detalle-partido)", cancellationToken);
        var partidoId = await CrearPartidoAsync(client, jornadaId, equipoIds[0], equipoIds[1], equipoNombres[0], cancellationToken);
        var sedeId = await CrearSedeAsync(client, "Pabellon Detalle (detalle-partido)", cancellationToken);
        await AsignarSedeAPartidoAsync(client, partidoId, equipoIds[0], equipoIds[1], sedeId, cancellationToken);
        await CrearParcialAsync(client, partidoId, periodo: 1, puntosLocal: 20, puntosVisitante: 18, cancellationToken);
        await MarcarPartidoResueltoAsync(client, partidoId, equipoIds[0], puntosLocal: 2, puntosVisitante: 0, cancellationToken);

        using var anonimo = fixture.CreateAnonymousWebHttpClient();
        var html = await (await anonimo.GetAsync($"/partidos/{partidoId}", cancellationToken)).Content.ReadAsStringAsync(cancellationToken);

        Assert.Contains(equipoNombres[0], html);
        Assert.Contains(equipoNombres[1], html);
        Assert.Contains("Pabellon Detalle (detalle-partido)", html);
        Assert.Contains("2-0", html);
        Assert.Contains("Resuelto", html);
        Assert.Contains("Incomparecencia", html);
        Assert.Contains("<td>20</td>", html);
        Assert.Contains("<td>18</td>", html);
        Assert.Contains($"/equipos/{equipoIds[0]}", html);
        Assert.Contains($"/sedes/{sedeId}", html);

        using var respuesta404 = await anonimo.GetAsync("/partidos/999999", cancellationToken);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, respuesta404.StatusCode);
    }

    [Fact]
    public async Task ResultadosPorEquipoMuestraHistoricoComoLocalYVisitante()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = fixture.CreateWebHttpClient();
        await LoginAsync(client, cancellationToken);

        var (competicionId, equipoIds, equipoNombres, _, _) = await CrearCompeticionConEquiposAsync(client, "resultados-equipo", numeroDeEquipos: 3, cancellationToken);
        var jornada1 = await CrearJornadaAsync(client, competicionId, numero: 1, etiqueta: "Jornada 1 (resultados-equipo)", cancellationToken);
        var jornada2 = await CrearJornadaAsync(client, competicionId, numero: 2, etiqueta: "Jornada 2 (resultados-equipo)", cancellationToken);

        // El equipo 0 juega como local en la jornada 1 y como visitante en la 2.
        var partidoLocal = await CrearPartidoAsync(client, jornada1, equipoIds[0], equipoIds[1], equipoNombres[0], cancellationToken);
        await CrearPartidoAsync(client, jornada2, equipoIds[2], equipoIds[0], equipoNombres[2], cancellationToken);

        using var anonimo = fixture.CreateAnonymousWebHttpClient();
        var html = await (await anonimo.GetAsync($"/equipos/{equipoIds[0]}/resultados", cancellationToken)).Content.ReadAsStringAsync(cancellationToken);

        Assert.Contains(equipoNombres[1], html); // rival como local
        Assert.Contains(equipoNombres[2], html); // rival como visitante
        Assert.Contains("Local", html);
        Assert.Contains("Visitante", html);
        Assert.Contains($"/partidos/{partidoLocal}", html);

        using var respuesta404 = await anonimo.GetAsync("/equipos/999999/resultados", cancellationToken);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, respuesta404.StatusCode);
    }

    [Fact]
    public async Task LasPaginasPublicasLlevanOutputCachingActivo()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = fixture.CreateWebHttpClient();
        await LoginAsync(client, cancellationToken);

        var (competicionId, _, _, _, _) = await CrearCompeticionConEquiposAsync(client, "cache-calendario", numeroDeEquipos: 0, cancellationToken);

        using var anonimo = fixture.CreateAnonymousWebHttpClient();
        using var primera = await anonimo.GetAsync($"/competiciones/{competicionId}/calendario", cancellationToken);
        using var segunda = await anonimo.GetAsync($"/competiciones/{competicionId}/calendario", cancellationToken);

        Assert.False(primera.Headers.Contains("Age"));
        Assert.True(segunda.Headers.Contains("Age"));
    }
}
