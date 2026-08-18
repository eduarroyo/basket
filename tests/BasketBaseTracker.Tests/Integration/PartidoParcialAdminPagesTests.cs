using static BasketBaseTracker.Tests.Integration.AdminHttpTestHelpers;

namespace BasketBaseTracker.Tests.Integration;

// Cubre PartidoParcial de BAS-10 vía HTTP real contra el AppHost completo: alta,
// edición, baja con confirmación y la restricción única (PartidoId, NumeroPeriodo).
public class PartidoParcialAdminPagesTests(AppHostSqlFixture fixture) : IClassFixture<AppHostSqlFixture>
{
    private static async Task<string> CrearPartidoDePruebaAsync(HttpClient client, string sufijo, CancellationToken cancellationToken)
    {
        var (competicionId, equipoIds, equipoNombres) = await CrearCompeticionConEquiposAsync(client, sufijo, numeroDeEquipos: 2, cancellationToken);
        var jornadaId = await CrearJornadaAsync(client, competicionId, numero: 1, etiqueta: $"Jornada 1 ({sufijo})", cancellationToken);
        return await CrearPartidoAsync(client, jornadaId, equipoIds[0], equipoIds[1], equipoNombres[0], cancellationToken);
    }

    [Fact]
    public async Task AltaListadoEdicionYBaja()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = fixture.CreateWebHttpClient();
        await LoginAsync(client, cancellationToken);

        var partidoId = await CrearPartidoDePruebaAsync(client, "parcial-alta", cancellationToken);

        // Alta.
        var createPage = await client.GetAsync($"/Admin/PartidoParcial/Create/{partidoId}", cancellationToken);
        var createToken = await GetAntiforgeryTokenAsync(createPage, cancellationToken);
        using var createResponse = await client.PostAsync(
            $"/Admin/PartidoParcial/Create/{partidoId}",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = createToken,
                ["PartidoParcial.NumeroPeriodo"] = "1",
                ["PartidoParcial.PuntosLocal"] = "20",
                ["PartidoParcial.PuntosVisitante"] = "18",
            }),
            cancellationToken);
        var indexHtml = await createResponse.Content.ReadAsStringAsync(cancellationToken);

        Assert.Equal($"/Admin/PartidoParcial/{partidoId}", createResponse.RequestMessage?.RequestUri?.AbsolutePath);
        Assert.Contains("<td>20</td>", indexHtml);

        var parcialId = ExtraerId(indexHtml, "PartidoParcial", "20");

        // Edición.
        var editUrl = $"/Admin/PartidoParcial/Edit/{parcialId}";
        var editPage = await client.GetAsync(editUrl, cancellationToken);
        var editToken = await GetAntiforgeryTokenAsync(editPage, cancellationToken);
        using var editResponse = await client.PostAsync(
            editUrl,
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = editToken,
                ["PartidoParcial.Id"] = parcialId,
                ["PartidoParcial.PartidoId"] = partidoId,
                ["PartidoParcial.NumeroPeriodo"] = "1",
                ["PartidoParcial.PuntosLocal"] = "22",
                ["PartidoParcial.PuntosVisitante"] = "18",
            }),
            cancellationToken);
        var afterEditHtml = await editResponse.Content.ReadAsStringAsync(cancellationToken);

        Assert.Contains("<td>22</td>", afterEditHtml);

        // Baja: página de confirmación primero, luego el POST que borra de verdad.
        var deleteUrl = $"/Admin/PartidoParcial/Delete/{parcialId}";
        var deletePage = await client.GetAsync(deleteUrl, cancellationToken);
        var deletePageHtml = await deletePage.Content.ReadAsStringAsync(cancellationToken);
        Assert.Contains("<dd class=\"col-sm-8\">22</dd>", deletePageHtml);
        var deleteToken = await GetAntiforgeryTokenAsync(deletePage, cancellationToken);

        using var deleteResponse = await client.PostAsync(deleteUrl, new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = deleteToken,
            ["PartidoParcial.Id"] = parcialId,
        }), cancellationToken);
        var afterDeleteHtml = await deleteResponse.Content.ReadAsStringAsync(cancellationToken);

        Assert.Equal($"/Admin/PartidoParcial/{partidoId}", deleteResponse.RequestMessage?.RequestUri?.AbsolutePath);
        Assert.DoesNotContain("<td>22</td>", afterDeleteHtml);
    }

    [Fact]
    public async Task RechazaPeriodoDuplicadoEnElMismoPartido()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = fixture.CreateWebHttpClient();
        await LoginAsync(client, cancellationToken);

        var partidoId = await CrearPartidoDePruebaAsync(client, "parcial-duplicado", cancellationToken);

        async Task<HttpResponseMessage> CrearParcialAsync()
        {
            var createPage = await client.GetAsync($"/Admin/PartidoParcial/Create/{partidoId}", cancellationToken);
            var token = await GetAntiforgeryTokenAsync(createPage, cancellationToken);

            return await client.PostAsync(
                $"/Admin/PartidoParcial/Create/{partidoId}",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["__RequestVerificationToken"] = token,
                    ["PartidoParcial.NumeroPeriodo"] = "2",
                    ["PartidoParcial.PuntosLocal"] = "15",
                    ["PartidoParcial.PuntosVisitante"] = "12",
                }),
                cancellationToken);
        }

        using var primeraRespuesta = await CrearParcialAsync();
        Assert.Equal($"/Admin/PartidoParcial/{partidoId}", primeraRespuesta.RequestMessage?.RequestUri?.AbsolutePath);

        using var segundaRespuesta = await CrearParcialAsync();
        var segundaHtml = await segundaRespuesta.Content.ReadAsStringAsync(cancellationToken);

        Assert.Equal($"/Admin/PartidoParcial/Create/{partidoId}", segundaRespuesta.RequestMessage?.RequestUri?.AbsolutePath);
        Assert.Contains("Ya existe un parcial para ese periodo en este partido.", segundaHtml);
    }
}
