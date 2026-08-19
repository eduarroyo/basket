using BasketBaseTracker.Web.Data;
using BasketBaseTracker.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;
using static BasketBaseTracker.Tests.Integration.AdminHttpTestHelpers;

namespace BasketBaseTracker.Tests.Integration;

// Cubre la pantalla Resultados de BAS-10 (Estado, marcador, motivo/ganador de una
// resolución administrativa) vía HTTP real contra el AppHost completo: marcar
// jugado, resolver administrativamente, revertir a programado, los rechazos de
// ResultadoReglas y el conflicto de concurrencia (RowVersion) sobre Partido.
public class ResultadoAdminPagesTests(AppHostSqlFixture fixture) : IClassFixture<AppHostSqlFixture>
{
    private static async Task<(string JornadaId, string PartidoId, string EquipoLocalId, string EquipoVisitanteId)> CrearPartidoDePruebaAsync(
        HttpClient client, string sufijo, CancellationToken cancellationToken)
    {
        var (competicionId, equipoIds, equipoNombres, _, _) = await CrearCompeticionConEquiposAsync(client, sufijo, numeroDeEquipos: 2, cancellationToken);
        var jornadaId = await CrearJornadaAsync(client, competicionId, numero: 1, etiqueta: $"Jornada 1 ({sufijo})", cancellationToken);
        var partidoId = await CrearPartidoAsync(client, jornadaId, equipoIds[0], equipoIds[1], equipoNombres[0], cancellationToken);

        return (jornadaId, partidoId, equipoIds[0], equipoIds[1]);
    }

    private static async Task<string> ObtenerRowVersionAsync(HttpResponseMessage editPage, CancellationToken cancellationToken)
    {
        var html = await editPage.Content.ReadAsStringAsync(cancellationToken);
        return ExtraerCampoOculto(html, "Partido.RowVersion");
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
    public async Task MarcarJugadoConMarcadorValido()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = fixture.CreateWebHttpClient();
        await LoginAsync(client, cancellationToken);

        var (_, partidoId, _, _) = await CrearPartidoDePruebaAsync(client, "resultado-jugado", cancellationToken);
        var editUrl = $"/Admin/Resultado/Edit/{partidoId}";

        var editPage = await client.GetAsync(editUrl, cancellationToken);
        var token = await GetAntiforgeryTokenAsync(editPage, cancellationToken);
        var rowVersion = await ObtenerRowVersionAsync(editPage, cancellationToken);

        using var response = await client.PostAsync(
            editUrl,
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["Partido.Id"] = partidoId,
                ["Partido.RowVersion"] = rowVersion,
                ["Partido.Estado"] = "1",
                ["Partido.PuntosLocal"] = "85",
                ["Partido.PuntosVisitante"] = "80",
                ["Partido.Observaciones"] = "",
            }),
            cancellationToken);

        Assert.True(response.RequestMessage?.RequestUri?.AbsolutePath?.StartsWith("/Admin/Partido/", StringComparison.Ordinal));

        var partidoIndexHtml = await response.Content.ReadAsStringAsync(cancellationToken);
        Assert.Contains("Jugado", partidoIndexHtml);
        Assert.Contains("85-80", partidoIndexHtml);
    }

    [Fact]
    public async Task RechazaMarcadorEmpatado()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = fixture.CreateWebHttpClient();
        await LoginAsync(client, cancellationToken);

        var (_, partidoId, _, _) = await CrearPartidoDePruebaAsync(client, "resultado-empate", cancellationToken);
        var editUrl = $"/Admin/Resultado/Edit/{partidoId}";

        var editPage = await client.GetAsync(editUrl, cancellationToken);
        var token = await GetAntiforgeryTokenAsync(editPage, cancellationToken);
        var rowVersion = await ObtenerRowVersionAsync(editPage, cancellationToken);

        using var response = await client.PostAsync(
            editUrl,
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["Partido.Id"] = partidoId,
                ["Partido.RowVersion"] = rowVersion,
                ["Partido.Estado"] = "1",
                ["Partido.PuntosLocal"] = "80",
                ["Partido.PuntosVisitante"] = "80",
            }),
            cancellationToken);
        var html = await response.Content.ReadAsStringAsync(cancellationToken);

        Assert.Equal(editUrl, response.RequestMessage?.RequestUri?.AbsolutePath);
        Assert.Contains("empate", html);
    }

    [Fact]
    public async Task ResuelveAdministrativamenteConMotivoYGanador()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = fixture.CreateWebHttpClient();
        await LoginAsync(client, cancellationToken);

        var (_, partidoId, equipoLocalId, _) = await CrearPartidoDePruebaAsync(client, "resultado-resuelto", cancellationToken);
        var editUrl = $"/Admin/Resultado/Edit/{partidoId}";

        var editPage = await client.GetAsync(editUrl, cancellationToken);
        var token = await GetAntiforgeryTokenAsync(editPage, cancellationToken);
        var rowVersion = await ObtenerRowVersionAsync(editPage, cancellationToken);

        using var response = await client.PostAsync(
            editUrl,
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["Partido.Id"] = partidoId,
                ["Partido.RowVersion"] = rowVersion,
                ["Partido.Estado"] = "4",
                ["Partido.MotivoResolucion"] = "0",
                ["Partido.EquipoGanadorResolucionId"] = equipoLocalId,
                ["Partido.PuntosLocal"] = "2",
                ["Partido.PuntosVisitante"] = "0",
            }),
            cancellationToken);
        var html = await response.Content.ReadAsStringAsync(cancellationToken);

        Assert.True(response.RequestMessage?.RequestUri?.AbsolutePath?.StartsWith("/Admin/Partido/", StringComparison.Ordinal));
        Assert.Contains("Resuelto", html);
        Assert.Contains("2-0", html);
    }

    [Fact]
    public async Task RechazaResolucionSinMotivoNiGanador()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = fixture.CreateWebHttpClient();
        await LoginAsync(client, cancellationToken);

        var (_, partidoId, _, _) = await CrearPartidoDePruebaAsync(client, "resultado-sin-motivo", cancellationToken);
        var editUrl = $"/Admin/Resultado/Edit/{partidoId}";

        var editPage = await client.GetAsync(editUrl, cancellationToken);
        var token = await GetAntiforgeryTokenAsync(editPage, cancellationToken);
        var rowVersion = await ObtenerRowVersionAsync(editPage, cancellationToken);

        using var response = await client.PostAsync(
            editUrl,
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["Partido.Id"] = partidoId,
                ["Partido.RowVersion"] = rowVersion,
                ["Partido.Estado"] = "4",
                ["Partido.MotivoResolucion"] = "",
                ["Partido.EquipoGanadorResolucionId"] = "",
            }),
            cancellationToken);
        var html = await response.Content.ReadAsStringAsync(cancellationToken);

        Assert.Equal(editUrl, response.RequestMessage?.RequestUri?.AbsolutePath);
        Assert.Contains("motivo y un equipo ganador", html);
    }

    [Fact]
    public async Task RevertirAProgramadoLimpiaMarcadorMotivoYGanador()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = fixture.CreateWebHttpClient();
        await LoginAsync(client, cancellationToken);

        var (_, partidoId, equipoLocalId, _) = await CrearPartidoDePruebaAsync(client, "resultado-revertir", cancellationToken);
        var editUrl = $"/Admin/Resultado/Edit/{partidoId}";

        // Primero se resuelve administrativamente...
        var primeraPagina = await client.GetAsync(editUrl, cancellationToken);
        var primerToken = await GetAntiforgeryTokenAsync(primeraPagina, cancellationToken);
        var primerRowVersion = await ObtenerRowVersionAsync(primeraPagina, cancellationToken);
        using var primerGuardado = await client.PostAsync(
            editUrl,
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = primerToken,
                ["Partido.Id"] = partidoId,
                ["Partido.RowVersion"] = primerRowVersion,
                ["Partido.Estado"] = "4",
                ["Partido.MotivoResolucion"] = "2",
                ["Partido.EquipoGanadorResolucionId"] = equipoLocalId,
                ["Partido.PuntosLocal"] = "2",
                ["Partido.PuntosVisitante"] = "0",
            }),
            cancellationToken);
        primerGuardado.EnsureSuccessStatusCode();

        // ...y luego se revierte a Programado (Art. 44: alineación indebida sin
        // mala fe, data-model.md).
        var segundaPagina = await client.GetAsync(editUrl, cancellationToken);
        var segundoToken = await GetAntiforgeryTokenAsync(segundaPagina, cancellationToken);
        var segundoRowVersion = await ObtenerRowVersionAsync(segundaPagina, cancellationToken);
        using var segundoGuardado = await client.PostAsync(
            editUrl,
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = segundoToken,
                ["Partido.Id"] = partidoId,
                ["Partido.RowVersion"] = segundoRowVersion,
                ["Partido.Estado"] = "0",
                ["Partido.Observaciones"] = "Alineación indebida sin mala fe: se repite sin ese jugador.",
            }),
            cancellationToken);
        segundoGuardado.EnsureSuccessStatusCode();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(fixture.ConnectionString).Options;
        await using var context = new ApplicationDbContext(options);
        var partido = await context.Partidos.SingleAsync(p => p.Id == int.Parse(partidoId), cancellationToken);

        Assert.Equal(PartidoEstado.Programado, partido.Estado);
        Assert.Null(partido.PuntosLocal);
        Assert.Null(partido.PuntosVisitante);
        Assert.Null(partido.MotivoResolucion);
        Assert.Null(partido.EquipoGanadorResolucionId);
    }

    [Fact]
    public async Task ConflictoDeConcurrencia()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = fixture.CreateWebHttpClient();
        await LoginAsync(client, cancellationToken);

        var (_, partidoId, _, _) = await CrearPartidoDePruebaAsync(client, "resultado-concurrencia", cancellationToken);
        var editUrl = $"/Admin/Resultado/Edit/{partidoId}";

        // Dos administradores cargan el formulario antes de que ninguno guarde.
        var primeraEdicionPage = await client.GetAsync(editUrl, cancellationToken);
        var primeraEdicionToken = await GetAntiforgeryTokenAsync(primeraEdicionPage, cancellationToken);
        var rowVersionOriginal = await ObtenerRowVersionAsync(primeraEdicionPage, cancellationToken);

        using var primerGuardado = await client.PostAsync(
            editUrl,
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = primeraEdicionToken,
                ["Partido.Id"] = partidoId,
                ["Partido.RowVersion"] = rowVersionOriginal,
                ["Partido.Estado"] = "1",
                ["Partido.PuntosLocal"] = "70",
                ["Partido.PuntosVisitante"] = "60",
            }),
            cancellationToken);
        Assert.True(primerGuardado.RequestMessage?.RequestUri?.AbsolutePath?.StartsWith("/Admin/Partido/", StringComparison.Ordinal));

        using var segundoGuardado = await client.PostAsync(
            editUrl,
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = primeraEdicionToken,
                ["Partido.Id"] = partidoId,
                ["Partido.RowVersion"] = rowVersionOriginal,
                ["Partido.Estado"] = "1",
                ["Partido.PuntosLocal"] = "50",
                ["Partido.PuntosVisitante"] = "90",
            }),
            cancellationToken);
        var segundoGuardadoHtml = await segundoGuardado.Content.ReadAsStringAsync(cancellationToken);

        Assert.Equal(editUrl, segundoGuardado.RequestMessage?.RequestUri?.AbsolutePath);
        Assert.Contains("Este partido se ha modificado en otro sitio mientras tanto.", segundoGuardadoHtml);
    }

    // Extrae la etiqueta de apertura completa de un elemento a partir de un
    // fragmento de su contenido (id, atributo...) — evita depender del espaciado
    // exacto del .cshtml al comprobar atributos como "disabled".
    private static string ExtraerEtiquetaDeApertura(string html, string fragmento)
    {
        var inicioFragmento = html.IndexOf(fragmento, StringComparison.Ordinal);
        Assert.True(inicioFragmento >= 0, $"No se encontró '{fragmento}' en el HTML.");
        var inicioEtiqueta = html.LastIndexOf('<', inicioFragmento);
        var finEtiqueta = html.IndexOf('>', inicioFragmento);
        return html[inicioEtiqueta..(finEtiqueta + 1)];
    }

    [Fact]
    public async Task MuestraLaSumaDeParcialesYElBotonHabilitadoCuandoHayParciales()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = fixture.CreateWebHttpClient();
        await LoginAsync(client, cancellationToken);

        var (_, partidoId, equipoLocalId, equipoVisitanteId) = await CrearPartidoDePruebaAsync(client, "resultado-atajo-parciales", cancellationToken);
        await CrearParcialAsync(client, partidoId, periodo: 1, puntosLocal: 20, puntosVisitante: 18, cancellationToken);
        await CrearParcialAsync(client, partidoId, periodo: 2, puntosLocal: 22, puntosVisitante: 20, cancellationToken);

        var editPage = await client.GetAsync($"/Admin/Resultado/Edit/{partidoId}", cancellationToken);
        var html = await editPage.Content.ReadAsStringAsync(cancellationToken);

        var etiquetaBoton = ExtraerEtiquetaDeApertura(html, "id=\"btnCalcularParciales\"");
        Assert.Contains("data-suma-local=\"42\"", etiquetaBoton);
        Assert.Contains("data-suma-visitante=\"38\"", etiquetaBoton);
        Assert.DoesNotContain("disabled", etiquetaBoton);

        var etiquetaGanador = ExtraerEtiquetaDeApertura(html, "id=\"ganadorSelect\"");
        Assert.Contains($"data-equipo-local-id=\"{equipoLocalId}\"", etiquetaGanador);
        Assert.Contains($"data-equipo-visitante-id=\"{equipoVisitanteId}\"", etiquetaGanador);
    }

    [Fact]
    public async Task ElBotonDeParcialesApareceDeshabilitadoSinParciales()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = fixture.CreateWebHttpClient();
        await LoginAsync(client, cancellationToken);

        var (_, partidoId, _, _) = await CrearPartidoDePruebaAsync(client, "resultado-sin-parciales", cancellationToken);

        var editPage = await client.GetAsync($"/Admin/Resultado/Edit/{partidoId}", cancellationToken);
        var html = await editPage.Content.ReadAsStringAsync(cancellationToken);

        var etiquetaBoton = ExtraerEtiquetaDeApertura(html, "id=\"btnCalcularParciales\"");
        Assert.Contains("data-suma-local=\"0\"", etiquetaBoton);
        Assert.Contains("data-suma-visitante=\"0\"", etiquetaBoton);
        Assert.Contains("disabled", etiquetaBoton);
    }
}
