using System.Text.RegularExpressions;
using static BasketBaseTracker.Tests.Integration.AdminHttpTestHelpers;

namespace BasketBaseTracker.Tests.Integration;

// Cubre la plantilla de equipo de BAS-8 (FichaJugador) vía HTTP real contra el
// AppHost completo: alta, listado, edición, baja con confirmación y la
// restricción única de dorsal dentro de un mismo equipo.
public class PlantillaAdminPagesTests(AppHostSqlFixture fixture) : IClassFixture<AppHostSqlFixture>
{
    private static async Task<string> CrearEquipoDePruebaAsync(HttpClient client, string sufijo, CancellationToken cancellationToken)
    {
        async Task<string> CrearAsync(string pagina, string entidad, Dictionary<string, string> campos)
        {
            var createPage = await client.GetAsync($"/Admin/{pagina}/Create", cancellationToken);
            var token = await GetAntiforgeryTokenAsync(createPage, cancellationToken);
            campos["__RequestVerificationToken"] = token;

            var response = await client.PostAsync($"/Admin/{pagina}/Create", new FormUrlEncodedContent(campos), cancellationToken);
            var html = await response.Content.ReadAsStringAsync(cancellationToken);

            var referencia = campos.First(c => c.Key.EndsWith(".Nombre", StringComparison.Ordinal)).Value;
            return ExtraerId(html, entidad, referencia);
        }

        var temporadaId = await CrearAsync("Temporada", "Temporada", new Dictionary<string, string>
        {
            ["Temporada.Nombre"] = $"2025-2026 ({sufijo})",
            ["Temporada.FechaInicio"] = "2025-09-01",
            ["Temporada.FechaFin"] = "2026-06-30",
            ["Temporada.Estado"] = "0",
        });
        var categoriaId = await CrearAsync("Categoria", "Categoria", new Dictionary<string, string>
        {
            ["Categoria.Nombre"] = $"Cadete ({sufijo})",
            ["Categoria.Orden"] = "1",
        });
        var clubId = await CrearAsync("Club", "Club", new Dictionary<string, string>
        {
            ["Club.Nombre"] = $"CB Prueba ({sufijo})",
            ["Club.Municipio"] = "Sevilla",
            ["Club.FechaAlta"] = "2020-01-01",
        });

        var createCompeticionPage = await client.GetAsync("/Admin/Competicion/Create", cancellationToken);
        var createCompeticionToken = await GetAntiforgeryTokenAsync(createCompeticionPage, cancellationToken);
        var competicionResponse = await client.PostAsync(
            "/Admin/Competicion/Create",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = createCompeticionToken,
                ["Competicion.TemporadaId"] = temporadaId,
                ["Competicion.CategoriaId"] = categoriaId,
                ["Competicion.PuntosVictoria"] = "2",
                ["Competicion.PuntosDerrota"] = "1",
            }),
            cancellationToken);
        var competicionHtml = await competicionResponse.Content.ReadAsStringAsync(cancellationToken);
        var competicionId = ExtraerId(competicionHtml, "Competicion", $"2025-2026 ({sufijo})");

        var createEquipoPage = await client.GetAsync("/Admin/Equipo/Create", cancellationToken);
        var createEquipoToken = await GetAntiforgeryTokenAsync(createEquipoPage, cancellationToken);
        var equipoNombre = $"CB Prueba A ({sufijo})";
        var equipoResponse = await client.PostAsync(
            "/Admin/Equipo/Create",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = createEquipoToken,
                ["Equipo.CompeticionId"] = competicionId,
                ["Equipo.ClubId"] = clubId,
                ["Equipo.Nombre"] = equipoNombre,
                ["Equipo.Estado"] = "0",
            }),
            cancellationToken);
        var equipoHtml = await equipoResponse.Content.ReadAsStringAsync(cancellationToken);

        return ExtraerId(equipoHtml, "Equipo", equipoNombre);
    }

    [Fact]
    public async Task FichaAltaListadoEdicionYBaja()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = fixture.CreateWebHttpClient();
        await LoginAsync(client, cancellationToken);

        var equipoId = await CrearEquipoDePruebaAsync(client, "plantilla-alta", cancellationToken);

        // Alta.
        var createPage = await client.GetAsync($"/Admin/FichaJugador/Create/{equipoId}", cancellationToken);
        var createToken = await GetAntiforgeryTokenAsync(createPage, cancellationToken);
        using var createResponse = await client.PostAsync(
            $"/Admin/FichaJugador/Create/{equipoId}",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = createToken,
                ["FichaJugador.Dorsal"] = "7",
                ["FichaJugador.Posicion"] = "0",
            }),
            cancellationToken);
        var indexHtml = await createResponse.Content.ReadAsStringAsync(cancellationToken);

        Assert.Equal($"/Admin/FichaJugador/{equipoId}", createResponse.RequestMessage?.RequestUri?.AbsolutePath);
        Assert.Contains("<td>7</td>", indexHtml);

        var fichaId = Regex.Match(indexHtml, """/Admin/FichaJugador/Edit/(?<id>\d+)""").Groups["id"].Value;

        // Edición.
        var editUrl = $"/Admin/FichaJugador/Edit/{fichaId}";
        var editPage = await client.GetAsync(editUrl, cancellationToken);
        var editToken = await GetAntiforgeryTokenAsync(editPage, cancellationToken);
        using var editResponse = await client.PostAsync(
            editUrl,
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = editToken,
                ["FichaJugador.Id"] = fichaId,
                ["FichaJugador.EquipoId"] = equipoId,
                ["FichaJugador.Dorsal"] = "9",
                ["FichaJugador.Posicion"] = "2",
            }),
            cancellationToken);
        var afterEditHtml = await editResponse.Content.ReadAsStringAsync(cancellationToken);

        Assert.Contains("<td>9</td>", afterEditHtml);
        Assert.Contains("Alero", afterEditHtml);

        // Baja: la página de confirmación primero, luego el POST que borra de verdad.
        var deleteUrl = $"/Admin/FichaJugador/Delete/{fichaId}";
        var deletePage = await client.GetAsync(deleteUrl, cancellationToken);
        var deletePageHtml = await deletePage.Content.ReadAsStringAsync(cancellationToken);
        Assert.Contains("<dd class=\"col-sm-8\">9</dd>", deletePageHtml);
        var deleteToken = await GetAntiforgeryTokenAsync(deletePage, cancellationToken);

        using var deleteResponse = await client.PostAsync(deleteUrl, new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = deleteToken,
            ["FichaJugador.Id"] = fichaId,
        }), cancellationToken);
        var afterDeleteHtml = await deleteResponse.Content.ReadAsStringAsync(cancellationToken);

        Assert.Equal($"/Admin/FichaJugador/{equipoId}", deleteResponse.RequestMessage?.RequestUri?.AbsolutePath);
        Assert.DoesNotContain("<td>9</td>", afterDeleteHtml);
    }

    [Fact]
    public async Task FichaRechazaDorsalDuplicadoEnElMismoEquipo()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = fixture.CreateWebHttpClient();
        await LoginAsync(client, cancellationToken);

        var equipoId = await CrearEquipoDePruebaAsync(client, "plantilla-duplicada", cancellationToken);

        async Task<HttpResponseMessage> CrearFichaAsync()
        {
            var createPage = await client.GetAsync($"/Admin/FichaJugador/Create/{equipoId}", cancellationToken);
            var token = await GetAntiforgeryTokenAsync(createPage, cancellationToken);

            return await client.PostAsync(
                $"/Admin/FichaJugador/Create/{equipoId}",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["__RequestVerificationToken"] = token,
                    ["FichaJugador.Dorsal"] = "4",
                    ["FichaJugador.Posicion"] = "",
                }),
                cancellationToken);
        }

        using var primeraRespuesta = await CrearFichaAsync();
        Assert.Equal($"/Admin/FichaJugador/{equipoId}", primeraRespuesta.RequestMessage?.RequestUri?.AbsolutePath);

        using var segundaRespuesta = await CrearFichaAsync();
        var segundaHtml = await segundaRespuesta.Content.ReadAsStringAsync(cancellationToken);

        Assert.Equal($"/Admin/FichaJugador/Create/{equipoId}", segundaRespuesta.RequestMessage?.RequestUri?.AbsolutePath);
        Assert.Contains("Ya existe un jugador con ese dorsal en este equipo.", segundaHtml);
    }
}
