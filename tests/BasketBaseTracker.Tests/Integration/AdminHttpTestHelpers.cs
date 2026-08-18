using System.Net;
using System.Text.RegularExpressions;

namespace BasketBaseTracker.Tests.Integration;

// Helpers compartidos por los tests de integración HTTP del área Admin (login,
// token antifalsificación) — extraído de CatalogoAdminPagesTests (BAS-6) al añadir
// GestionAnualAdminPagesTests (BAS-7) para no duplicarlos.
public static partial class AdminHttpTestHelpers
{
    [GeneratedRegex("""name="__RequestVerificationToken"[^>]*?value="(?<token>[^"]+)"[^>]*>""")]
    private static partial Regex AntiforgeryTokenRegex();

    public static async Task<string> GetAntiforgeryTokenAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var html = await response.Content.ReadAsStringAsync(cancellationToken);
        var match = AntiforgeryTokenRegex().Match(html);
        Assert.True(match.Success, $"No se encontró el token antifalsificación en la respuesta de {response.RequestMessage?.RequestUri}.");
        return match.Groups["token"].Value;
    }

    public static async Task LoginAsync(HttpClient client, CancellationToken cancellationToken)
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

    // Busca el enlace de edición dentro de la MISMA fila <tr> que contiene el texto
    // de referencia, no el primero de toda la página — con IClassFixture compartida
    // entre los [Fact] de una misma clase (y xUnit ejecutándolos en paralelo por
    // defecto), el listado puede contener filas creadas por otro test concurrente,
    // así que "el primer enlace de edición de la página" no es fiable (BAS-7:
    // fallo intermitente al ejecutar la clase completa, pero no en aislamiento).
    public static string ExtraerId(string indexHtml, string entidad, string textoDeReferencia)
    {
        Assert.Contains(textoDeReferencia, indexHtml);
        var pattern = $"""<tr>(?:(?!</tr>)[\s\S])*?{Regex.Escape(textoDeReferencia)}(?:(?!</tr>)[\s\S])*?/Admin/{entidad}/Edit/(?<id>\d+)""";
        var match = Regex.Match(indexHtml, pattern);
        Assert.True(match.Success, $"No se encontró la fila de '{textoDeReferencia}' con su enlace de edición en /Admin/{entidad}.");
        return match.Groups["id"].Value;
    }

    // Extrae el valor de un campo oculto (p. ej. RowVersion) decodificando entidades
    // HTML — Razor codifica caracteres del Base64 de un byte[] como '+' (a
    // "&#x2B;") en atributos, así que leer el atributo en crudo y reenviarlo tal
    // cual corrompe el valor real; solo se nota en el token de concurrencia cuando
    // el RowVersion en cuestión resulta contener uno de esos caracteres (BAS-10:
    // fallo intermitente en tests de concurrencia, solo al ejecutar la clase
    // completa, según qué RowVersion le tocara a cada test en paralelo).
    public static string ExtraerCampoOculto(string html, string nombreDeCampo)
    {
        var match = Regex.Match(html, $"""name="{Regex.Escape(nombreDeCampo)}"[^>]*?value="(?<valor>[^"]*)"[^>]*>""");
        Assert.True(match.Success, $"No se encontró el campo oculto '{nombreDeCampo}'.");
        return WebUtility.HtmlDecode(match.Groups["valor"].Value);
    }

    // Extraído de CalendarioAdminPagesTests (BAS-9) al añadir ResultadoAdminPagesTests
    // y PartidoParcialAdminPagesTests (BAS-10), que necesitan el mismo árbol
    // Temporada→Categoria→Competicion→Equipo→Jornada→Partido de partida.
    public static async Task<(string CompeticionId, string[] EquipoIds, string[] EquipoNombres, string TemporadaId, string[] ClubIds)> CrearCompeticionConEquiposAsync(
        HttpClient client, string sufijo, int numeroDeEquipos, CancellationToken cancellationToken, string temporadaEstado = "0")
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
            ["Temporada.Estado"] = temporadaEstado,
        });
        var categoriaId = await CrearAsync("Categoria", "Categoria", new Dictionary<string, string>
        {
            ["Categoria.Nombre"] = $"Cadete ({sufijo})",
            ["Categoria.Orden"] = "1",
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

        var equipoIds = new string[numeroDeEquipos];
        var equipoNombres = new string[numeroDeEquipos];
        var clubIds = new string[numeroDeEquipos];
        for (var i = 0; i < numeroDeEquipos; i++)
        {
            var clubId = await CrearAsync("Club", "Club", new Dictionary<string, string>
            {
                ["Club.Nombre"] = $"CB Prueba {i} ({sufijo})",
                ["Club.Municipio"] = "Sevilla",
                ["Club.FechaAlta"] = "2020-01-01",
            });
            clubIds[i] = clubId;

            var createEquipoPage = await client.GetAsync("/Admin/Equipo/Create", cancellationToken);
            var createEquipoToken = await GetAntiforgeryTokenAsync(createEquipoPage, cancellationToken);
            var equipoNombre = $"CB Prueba {i} A ({sufijo})";
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
            equipoIds[i] = ExtraerId(equipoHtml, "Equipo", equipoNombre);
            equipoNombres[i] = equipoNombre;
        }

        return (competicionId, equipoIds, equipoNombres, temporadaId, clubIds);
    }

    public static async Task<string> CrearJornadaAsync(
        HttpClient client, string competicionId, int numero, string etiqueta, CancellationToken cancellationToken, bool cuentaParaClasificacion = true)
    {
        var createPage = await client.GetAsync($"/Admin/Jornada/Create/{competicionId}", cancellationToken);
        var token = await GetAntiforgeryTokenAsync(createPage, cancellationToken);

        var response = await client.PostAsync(
            $"/Admin/Jornada/Create/{competicionId}",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["Jornada.Numero"] = numero.ToString(),
                ["Jornada.Etiqueta"] = etiqueta,
                ["Jornada.CuentaParaClasificacion"] = cuentaParaClasificacion ? "true" : "false",
            }),
            cancellationToken);
        var html = await response.Content.ReadAsStringAsync(cancellationToken);

        return ExtraerId(html, "Jornada", etiqueta);
    }

    // Nuevo en BAS-10: crea un partido programado (sin resultado) a partir de un
    // par de equipos ya existentes en la jornada indicada. equipoLocalNombre
    // identifica la fila del partido recién creado en el listado (ExtraerId).
    public static async Task<string> CrearPartidoAsync(
        HttpClient client, string jornadaId, string equipoLocalId, string equipoVisitanteId, string equipoLocalNombre, CancellationToken cancellationToken)
    {
        var createPage = await client.GetAsync($"/Admin/Partido/Create/{jornadaId}", cancellationToken);
        var token = await GetAntiforgeryTokenAsync(createPage, cancellationToken);

        var response = await client.PostAsync(
            $"/Admin/Partido/Create/{jornadaId}",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["Partido.EquipoLocalId"] = equipoLocalId,
                ["Partido.EquipoVisitanteId"] = equipoVisitanteId,
                ["Partido.SedeId"] = "",
                ["Partido.FechaHora"] = "",
            }),
            cancellationToken);
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync(cancellationToken);

        return ExtraerId(html, "Partido", equipoLocalNombre);
    }

    // Nuevo en BAS-11: marca un partido como Jugado con el marcador indicado, vía
    // /Admin/Resultado/Edit (BAS-10).
    public static async Task MarcarPartidoJugadoAsync(
        HttpClient client, string partidoId, int puntosLocal, int puntosVisitante, CancellationToken cancellationToken)
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
                ["Partido.Estado"] = "1", // Jugado
                ["Partido.PuntosLocal"] = puntosLocal.ToString(),
                ["Partido.PuntosVisitante"] = puntosVisitante.ToString(),
            }),
            cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
