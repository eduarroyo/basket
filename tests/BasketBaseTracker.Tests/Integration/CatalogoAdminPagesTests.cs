using System.Text.RegularExpressions;

namespace BasketBaseTracker.Tests.Integration;

// Cubre las páginas de catálogo de BAS-6 (Temporada, Categoria, Club, Sede): alta,
// listado y edición vía HTTP real contra el AppHost completo, más la redirección a
// Login para peticiones sin autenticar. No usa un cliente HTTP con mocks: cada
// HttpClient (AppHostSqlFixture.CreateWebHttpClient) gestiona cookies por sí solo
// (comportamiento por defecto de SocketsHttpHandler), así que la sesión de
// autenticación se mantiene entre peticiones del mismo test sin nada adicional.
public partial class CatalogoAdminPagesTests(AppHostSqlFixture fixture) : IClassFixture<AppHostSqlFixture>
{
    [GeneratedRegex("""name="__RequestVerificationToken"[^>]*?value="(?<token>[^"]+)"[^>]*>""")]
    private static partial Regex AntiforgeryTokenRegex();

    private static async Task<string> GetAntiforgeryTokenAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var html = await response.Content.ReadAsStringAsync(cancellationToken);
        var match = AntiforgeryTokenRegex().Match(html);
        Assert.True(match.Success, $"No se encontró el token antifalsificación en la respuesta de {response.RequestMessage?.RequestUri}.");
        return match.Groups["token"].Value;
    }

    private static async Task LoginAsync(HttpClient client, CancellationToken cancellationToken)
    {
        var loginPage = await client.GetAsync("/Admin/Login", cancellationToken);
        var token = await GetAntiforgeryTokenAsync(loginPage, cancellationToken);

        var response = await client.PostAsync(
            "/Admin/Login",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["Input.Email"] = AppHostSqlFixture.SeedAdminEmail,
                ["Input.Password"] = AppHostSqlFixture.SeedAdminPassword,
            }),
            cancellationToken);

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task PeticionAnonimaATemporadasRedirigeALogin()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = fixture.CreateAnonymousWebHttpClient();

        using var response = await client.GetAsync("/Admin/Temporada", cancellationToken);

        Assert.Equal("/Admin/Login", response.RequestMessage?.RequestUri?.AbsolutePath);
    }

    [Fact]
    public async Task TemporadaAltaListadoYEdicion()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = fixture.CreateWebHttpClient();
        await LoginAsync(client, cancellationToken);

        var createPage = await client.GetAsync("/Admin/Temporada/Create", cancellationToken);
        var createToken = await GetAntiforgeryTokenAsync(createPage, cancellationToken);

        using var createResponse = await client.PostAsync(
            "/Admin/Temporada/Create",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = createToken,
                ["Temporada.Nombre"] = "2025-2026",
                ["Temporada.FechaInicio"] = "2025-09-01",
                ["Temporada.FechaFin"] = "2026-06-30",
                ["Temporada.Estado"] = "0",
            }),
            cancellationToken);
        var indexHtml = await createResponse.Content.ReadAsStringAsync(cancellationToken);

        Assert.Equal("/Admin/Temporada", createResponse.RequestMessage?.RequestUri?.AbsolutePath);
        Assert.Contains("2025-2026", indexHtml);

        var idMatch = Regex.Match(indexHtml, """/Admin/Temporada/Edit/(?<id>\d+)""");
        Assert.True(idMatch.Success, "No se encontró el enlace de edición de la temporada recién creada.");
        var editUrl = $"/Admin/Temporada/Edit/{idMatch.Groups["id"].Value}";

        var editPage = await client.GetAsync(editUrl, cancellationToken);
        var editToken = await GetAntiforgeryTokenAsync(editPage, cancellationToken);

        using var editResponse = await client.PostAsync(
            editUrl,
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = editToken,
                ["Temporada.Id"] = idMatch.Groups["id"].Value,
                ["Temporada.Nombre"] = "2025-2026",
                ["Temporada.FechaInicio"] = "2025-09-01",
                ["Temporada.FechaFin"] = "2026-06-30",
                ["Temporada.Estado"] = "1",
            }),
            cancellationToken);
        var afterEditHtml = await editResponse.Content.ReadAsStringAsync(cancellationToken);

        Assert.Contains("EnCurso", afterEditHtml);
    }

    [Fact]
    public async Task CategoriaAltaListadoYEdicion()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = fixture.CreateWebHttpClient();
        await LoginAsync(client, cancellationToken);

        var createPage = await client.GetAsync("/Admin/Categoria/Create", cancellationToken);
        var createToken = await GetAntiforgeryTokenAsync(createPage, cancellationToken);

        using var createResponse = await client.PostAsync(
            "/Admin/Categoria/Create",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = createToken,
                ["Categoria.Nombre"] = "Cadete",
                ["Categoria.Orden"] = "4",
            }),
            cancellationToken);
        var indexHtml = await createResponse.Content.ReadAsStringAsync(cancellationToken);

        Assert.Equal("/Admin/Categoria", createResponse.RequestMessage?.RequestUri?.AbsolutePath);
        Assert.Contains("Cadete", indexHtml);

        var idMatch = Regex.Match(indexHtml, """/Admin/Categoria/Edit/(?<id>\d+)""");
        Assert.True(idMatch.Success, "No se encontró el enlace de edición de la categoría recién creada.");
        var editUrl = $"/Admin/Categoria/Edit/{idMatch.Groups["id"].Value}";

        var editPage = await client.GetAsync(editUrl, cancellationToken);
        var editToken = await GetAntiforgeryTokenAsync(editPage, cancellationToken);

        using var editResponse = await client.PostAsync(
            editUrl,
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = editToken,
                ["Categoria.Id"] = idMatch.Groups["id"].Value,
                ["Categoria.Nombre"] = "Cadete",
                ["Categoria.Orden"] = "5",
            }),
            cancellationToken);
        var afterEditHtml = await editResponse.Content.ReadAsStringAsync(cancellationToken);

        Assert.Contains("<td>5</td>", afterEditHtml);
    }

    [Fact]
    public async Task ClubAltaListadoYEdicion()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = fixture.CreateWebHttpClient();
        await LoginAsync(client, cancellationToken);

        var createPage = await client.GetAsync("/Admin/Club/Create", cancellationToken);
        var createToken = await GetAntiforgeryTokenAsync(createPage, cancellationToken);

        using var createResponse = await client.PostAsync(
            "/Admin/Club/Create",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = createToken,
                ["Club.Nombre"] = "CB Triana",
                ["Club.Municipio"] = "Sevilla",
                ["Club.FechaAlta"] = "2020-01-01",
            }),
            cancellationToken);
        var indexHtml = await createResponse.Content.ReadAsStringAsync(cancellationToken);

        Assert.Equal("/Admin/Club", createResponse.RequestMessage?.RequestUri?.AbsolutePath);
        Assert.Contains("CB Triana", indexHtml);

        var idMatch = Regex.Match(indexHtml, """/Admin/Club/Edit/(?<id>\d+)""");
        Assert.True(idMatch.Success, "No se encontró el enlace de edición del club recién creado.");
        var editUrl = $"/Admin/Club/Edit/{idMatch.Groups["id"].Value}";

        var editPage = await client.GetAsync(editUrl, cancellationToken);
        var editToken = await GetAntiforgeryTokenAsync(editPage, cancellationToken);

        using var editResponse = await client.PostAsync(
            editUrl,
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = editToken,
                ["Club.Id"] = idMatch.Groups["id"].Value,
                ["Club.Nombre"] = "CB Triana",
                ["Club.Municipio"] = "Dos Hermanas",
                ["Club.FechaAlta"] = "2020-01-01",
            }),
            cancellationToken);
        var afterEditHtml = await editResponse.Content.ReadAsStringAsync(cancellationToken);

        Assert.Contains("Dos Hermanas", afterEditHtml);
    }

    [Fact]
    public async Task SedeAltaListadoYEdicion()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = fixture.CreateWebHttpClient();
        await LoginAsync(client, cancellationToken);

        var createPage = await client.GetAsync("/Admin/Sede/Create", cancellationToken);
        var createToken = await GetAntiforgeryTokenAsync(createPage, cancellationToken);

        using var createResponse = await client.PostAsync(
            "/Admin/Sede/Create",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = createToken,
                ["Sede.Nombre"] = "Pabellon San Pablo",
                ["Sede.Municipio"] = "Sevilla",
                ["Sede.Direccion"] = "Calle Ejemplo 1",
            }),
            cancellationToken);
        var indexHtml = await createResponse.Content.ReadAsStringAsync(cancellationToken);

        Assert.Equal("/Admin/Sede", createResponse.RequestMessage?.RequestUri?.AbsolutePath);
        Assert.Contains("Pabellon San Pablo", indexHtml);

        var idMatch = Regex.Match(indexHtml, """/Admin/Sede/Edit/(?<id>\d+)""");
        Assert.True(idMatch.Success, "No se encontró el enlace de edición de la sede recién creada.");
        var editUrl = $"/Admin/Sede/Edit/{idMatch.Groups["id"].Value}";

        var editPage = await client.GetAsync(editUrl, cancellationToken);
        var editToken = await GetAntiforgeryTokenAsync(editPage, cancellationToken);

        using var editResponse = await client.PostAsync(
            editUrl,
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = editToken,
                ["Sede.Id"] = idMatch.Groups["id"].Value,
                ["Sede.Nombre"] = "Pabellon San Pablo",
                ["Sede.Municipio"] = "Sevilla",
                ["Sede.Direccion"] = "Calle Nueva 2",
            }),
            cancellationToken);
        var afterEditHtml = await editResponse.Content.ReadAsStringAsync(cancellationToken);

        Assert.Contains("Calle Nueva 2", afterEditHtml);
    }
}
