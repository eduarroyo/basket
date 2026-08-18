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
}
