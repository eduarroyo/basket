using System.Text.RegularExpressions;
using static BasketBaseTracker.Tests.Integration.AdminHttpTestHelpers;

namespace BasketBaseTracker.Tests.Integration;

// Cubre las páginas de "Gestión anual" de BAS-7 (Competicion, Equipo) vía HTTP real
// contra el AppHost completo: alta, listado, edición, la restricción única de
// Competicion (temporada+categoría) y la concurrencia optimista de Equipo
// (RowVersion) — la primera entidad de la app que la ejercita de verdad.
public class GestionAnualAdminPagesTests(AppHostSqlFixture fixture) : IClassFixture<AppHostSqlFixture>
{
    private static async Task<string> CrearTemporadaAsync(HttpClient client, string nombre, CancellationToken cancellationToken)
    {
        var createPage = await client.GetAsync("/Admin/Temporada/Create", cancellationToken);
        var token = await GetAntiforgeryTokenAsync(createPage, cancellationToken);

        var response = await client.PostAsync(
            "/Admin/Temporada/Create",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["Temporada.Nombre"] = nombre,
                ["Temporada.FechaInicio"] = "2025-09-01",
                ["Temporada.FechaFin"] = "2026-06-30",
                ["Temporada.Estado"] = "0",
            }),
            cancellationToken);
        var html = await response.Content.ReadAsStringAsync(cancellationToken);

        return ExtraerId(html, "Temporada", nombre);
    }

    private static async Task<string> CrearCategoriaAsync(HttpClient client, string nombre, CancellationToken cancellationToken)
    {
        var createPage = await client.GetAsync("/Admin/Categoria/Create", cancellationToken);
        var token = await GetAntiforgeryTokenAsync(createPage, cancellationToken);

        var response = await client.PostAsync(
            "/Admin/Categoria/Create",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["Categoria.Nombre"] = nombre,
                ["Categoria.Orden"] = "1",
            }),
            cancellationToken);
        var html = await response.Content.ReadAsStringAsync(cancellationToken);

        return ExtraerId(html, "Categoria", nombre);
    }

    private static async Task<string> CrearClubAsync(HttpClient client, string nombre, CancellationToken cancellationToken)
    {
        var createPage = await client.GetAsync("/Admin/Club/Create", cancellationToken);
        var token = await GetAntiforgeryTokenAsync(createPage, cancellationToken);

        var response = await client.PostAsync(
            "/Admin/Club/Create",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["Club.Nombre"] = nombre,
                ["Club.Municipio"] = "Sevilla",
                ["Club.FechaAlta"] = "2020-01-01",
            }),
            cancellationToken);
        var html = await response.Content.ReadAsStringAsync(cancellationToken);

        return ExtraerId(html, "Club", nombre);
    }

    // Busca el enlace de edición dentro de la MISMA fila <tr> que contiene el texto
    // de referencia, no el primero de toda la página — con IClassFixture compartida
    // entre los [Fact] de esta clase (y xUnit ejecutándolos en paralelo por
    // defecto), el listado puede contener filas creadas por otro test concurrente,
    // así que "el primer enlace de edición de la página" no es fiable (comprobado:
    // fallo intermitente al ejecutar la clase completa, pero no en aislamiento).
    private static string ExtraerId(string indexHtml, string entidad, string textoDeReferencia)
    {
        Assert.Contains(textoDeReferencia, indexHtml);
        var pattern = $"""<tr>(?:(?!</tr>)[\s\S])*?{Regex.Escape(textoDeReferencia)}(?:(?!</tr>)[\s\S])*?/Admin/{entidad}/Edit/(?<id>\d+)""";
        var match = Regex.Match(indexHtml, pattern);
        Assert.True(match.Success, $"No se encontró la fila de '{textoDeReferencia}' con su enlace de edición en /Admin/{entidad}.");
        return match.Groups["id"].Value;
    }

    [Fact]
    public async Task CompeticionAltaListadoYEdicion()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = fixture.CreateWebHttpClient();
        await LoginAsync(client, cancellationToken);

        var temporadaId = await CrearTemporadaAsync(client, "2025-2026 (competicion-alta)", cancellationToken);
        var categoriaId = await CrearCategoriaAsync(client, "Infantil (competicion-alta)", cancellationToken);

        var createPage = await client.GetAsync("/Admin/Competicion/Create", cancellationToken);
        var createToken = await GetAntiforgeryTokenAsync(createPage, cancellationToken);

        using var createResponse = await client.PostAsync(
            "/Admin/Competicion/Create",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = createToken,
                ["Competicion.TemporadaId"] = temporadaId,
                ["Competicion.CategoriaId"] = categoriaId,
                ["Competicion.PuntosVictoria"] = "2",
                ["Competicion.PuntosDerrota"] = "1",
            }),
            cancellationToken);
        var indexHtml = await createResponse.Content.ReadAsStringAsync(cancellationToken);

        Assert.Equal("/Admin/Competicion", createResponse.RequestMessage?.RequestUri?.AbsolutePath);
        Assert.Contains("2025-2026 (competicion-alta)", indexHtml);
        Assert.Contains("Infantil (competicion-alta)", indexHtml);

        var competicionId = ExtraerId(indexHtml, "Competicion", "2025-2026 (competicion-alta)");
        var editUrl = $"/Admin/Competicion/Edit/{competicionId}";

        var editPage = await client.GetAsync(editUrl, cancellationToken);
        var editToken = await GetAntiforgeryTokenAsync(editPage, cancellationToken);

        using var editResponse = await client.PostAsync(
            editUrl,
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = editToken,
                ["Competicion.Id"] = competicionId,
                ["Competicion.TemporadaId"] = temporadaId,
                ["Competicion.CategoriaId"] = categoriaId,
                ["Competicion.PuntosVictoria"] = "3",
                ["Competicion.PuntosDerrota"] = "1",
            }),
            cancellationToken);
        var afterEditHtml = await editResponse.Content.ReadAsStringAsync(cancellationToken);

        Assert.Contains("<td>3</td>", afterEditHtml);
    }

    [Fact]
    public async Task CompeticionRechazaTemporadaYCategoriaDuplicadas()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = fixture.CreateWebHttpClient();
        await LoginAsync(client, cancellationToken);

        var temporadaId = await CrearTemporadaAsync(client, "2025-2026 (competicion-duplicada)", cancellationToken);
        var categoriaId = await CrearCategoriaAsync(client, "Cadete (competicion-duplicada)", cancellationToken);

        async Task<HttpResponseMessage> CrearCompeticionAsync()
        {
            var createPage = await client.GetAsync("/Admin/Competicion/Create", cancellationToken);
            var token = await GetAntiforgeryTokenAsync(createPage, cancellationToken);

            return await client.PostAsync(
                "/Admin/Competicion/Create",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["__RequestVerificationToken"] = token,
                    ["Competicion.TemporadaId"] = temporadaId,
                    ["Competicion.CategoriaId"] = categoriaId,
                    ["Competicion.PuntosVictoria"] = "2",
                    ["Competicion.PuntosDerrota"] = "1",
                }),
                cancellationToken);
        }

        using var primeraRespuesta = await CrearCompeticionAsync();
        Assert.Equal("/Admin/Competicion", primeraRespuesta.RequestMessage?.RequestUri?.AbsolutePath);

        using var segundaRespuesta = await CrearCompeticionAsync();
        var segundaHtml = await segundaRespuesta.Content.ReadAsStringAsync(cancellationToken);

        Assert.Equal("/Admin/Competicion/Create", segundaRespuesta.RequestMessage?.RequestUri?.AbsolutePath);
        Assert.Contains("Ya existe una competici", segundaHtml);
    }

    [Fact]
    public async Task EquipoAltaListadoEdicionYConflictoDeConcurrencia()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = fixture.CreateWebHttpClient();
        await LoginAsync(client, cancellationToken);

        var temporadaId = await CrearTemporadaAsync(client, "2025-2026 (equipo)", cancellationToken);
        var categoriaId = await CrearCategoriaAsync(client, "Juvenil (equipo)", cancellationToken);
        var clubId = await CrearClubAsync(client, "CB Nervion (equipo)", cancellationToken);

        var createCompeticionPage = await client.GetAsync("/Admin/Competicion/Create", cancellationToken);
        var createCompeticionToken = await GetAntiforgeryTokenAsync(createCompeticionPage, cancellationToken);
        using var competicionResponse = await client.PostAsync(
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
        var competicionId = ExtraerId(competicionHtml, "Competicion", "2025-2026 (equipo)");

        // Alta.
        var createEquipoPage = await client.GetAsync("/Admin/Equipo/Create", cancellationToken);
        var createEquipoToken = await GetAntiforgeryTokenAsync(createEquipoPage, cancellationToken);
        using var equipoResponse = await client.PostAsync(
            "/Admin/Equipo/Create",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = createEquipoToken,
                ["Equipo.CompeticionId"] = competicionId,
                ["Equipo.ClubId"] = clubId,
                ["Equipo.Nombre"] = "CB Nervion A",
                ["Equipo.Estado"] = "0",
            }),
            cancellationToken);
        var equipoIndexHtml = await equipoResponse.Content.ReadAsStringAsync(cancellationToken);

        Assert.Equal("/Admin/Equipo", equipoResponse.RequestMessage?.RequestUri?.AbsolutePath);
        Assert.Contains("CB Nervion A", equipoIndexHtml);

        var equipoId = ExtraerId(equipoIndexHtml, "Equipo", "CB Nervion A");
        var editUrl = $"/Admin/Equipo/Edit/{equipoId}";

        // Dos administradores cargan el formulario de edición del mismo equipo antes
        // de que ninguno guarde — cada uno con el RowVersion original.
        var primeraEdicionPage = await client.GetAsync(editUrl, cancellationToken);
        var primeraEdicionHtml = await primeraEdicionPage.Content.ReadAsStringAsync(cancellationToken);
        var primeraEdicionToken = await GetAntiforgeryTokenAsync(primeraEdicionPage, cancellationToken);
        var rowVersionOriginal = Regex.Match(primeraEdicionHtml, "name=\"Equipo\\.RowVersion\"[^>]*value=\"(?<rv>[^\"]*)\"").Groups["rv"].Value;

        // El primer administrador guarda: éxito, y el RowVersion en la base de datos cambia.
        using var primerGuardado = await client.PostAsync(
            editUrl,
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = primeraEdicionToken,
                ["Equipo.Id"] = equipoId,
                ["Equipo.RowVersion"] = rowVersionOriginal,
                ["Equipo.CompeticionId"] = competicionId,
                ["Equipo.ClubId"] = clubId,
                ["Equipo.Nombre"] = "CB Nervion A (editado)",
                ["Equipo.Estado"] = "0",
            }),
            cancellationToken);
        Assert.Equal("/Admin/Equipo", primerGuardado.RequestMessage?.RequestUri?.AbsolutePath);

        // El segundo administrador, que seguía con el formulario abierto desde antes
        // del primer guardado (mismo token y mismo RowVersion de esa misma carga de
        // página), intenta guardar con el RowVersion ya obsoleto.
        using var segundoGuardado = await client.PostAsync(
            editUrl,
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = primeraEdicionToken,
                ["Equipo.Id"] = equipoId,
                ["Equipo.RowVersion"] = rowVersionOriginal,
                ["Equipo.CompeticionId"] = competicionId,
                ["Equipo.ClubId"] = clubId,
                ["Equipo.Nombre"] = "CB Nervion A (conflicto)",
                ["Equipo.Estado"] = "0",
            }),
            cancellationToken);
        var segundoGuardadoHtml = await segundoGuardado.Content.ReadAsStringAsync(cancellationToken);

        Assert.Equal(editUrl, segundoGuardado.RequestMessage?.RequestUri?.AbsolutePath);
        Assert.Contains("Este equipo se ha modificado en otro sitio mientras tanto.", segundoGuardadoHtml);
    }
}
