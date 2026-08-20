using System.Text.RegularExpressions;
using static BasketBaseTracker.Tests.Integration.AdminHttpTestHelpers;

namespace BasketBaseTracker.Tests.Integration;

// Cubre la Clasificación de BAS-11 (consulta calculada, sin tabla propia) vía HTTP
// real contra el AppHost completo: la vista admin y la pública muestran la misma
// tabla, las jornadas con CuentaParaClasificacion = false no cuentan, y la
// pública lleva Output Caching mientras que la admin no.
public class ClasificacionPagesTests(AppHostSqlFixture fixture) : IClassFixture<AppHostSqlFixture>
{
    private static string ExtraerFila(string html, string equipoNombre)
    {
        var pattern = $"""<tr>(?:(?!</tr>)[\s\S])*?{Regex.Escape(equipoNombre)}(?:(?!</tr>)[\s\S])*?</tr>""";
        var match = Regex.Match(html, pattern);
        Assert.True(match.Success, $"No se encontró la fila de '{equipoNombre}' en la tabla de clasificación.");
        return match.Value;
    }

    [Fact]
    public async Task MuestraLaTablaOrdenadaYExcluyeJornadasQueNoCuentanParaClasificacion()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = fixture.CreateWebHttpClient();
        await LoginAsync(client, cancellationToken);

        var (competicionId, equipoIds, equipoNombres, _, _) = await CrearCompeticionConEquiposAsync(client, "clasificacion", numeroDeEquipos: 2, cancellationToken);

        // Jornada que cuenta: A gana a B 80-70.
        var jornadaQueCuenta = await CrearJornadaAsync(client, competicionId, numero: 1, etiqueta: "Jornada 1 (clasificacion)", cancellationToken);
        var partido1 = await CrearPartidoAsync(client, jornadaQueCuenta, equipoIds[0], equipoIds[1], equipoNombres[0], cancellationToken);
        await MarcarPartidoJugadoAsync(client, partido1, puntosLocal: 80, puntosVisitante: 70, cancellationToken);

        // Jornada que NO cuenta: B gana a A 90-60 — no debe alterar la tabla.
        var jornadaQueNoCuenta = await CrearJornadaAsync(
            client, competicionId, numero: 2, etiqueta: "Jornada 2 (clasificacion)", cancellationToken, cuentaParaClasificacion: false);
        var partido2 = await CrearPartidoAsync(client, jornadaQueNoCuenta, equipoIds[1], equipoIds[0], equipoNombres[1], cancellationToken);
        await MarcarPartidoJugadoAsync(client, partido2, puntosLocal: 90, puntosVisitante: 60, cancellationToken);

        var adminHtml = await (await client.GetAsync($"/Admin/Clasificacion/{competicionId}", cancellationToken)).Content.ReadAsStringAsync(cancellationToken);

        // El equipo 0 (ganador de la única jornada que cuenta) debe ir primero.
        Assert.True(adminHtml.IndexOf(equipoNombres[0], StringComparison.Ordinal) < adminHtml.IndexOf(equipoNombres[1], StringComparison.Ordinal));

        // Si la jornada excluida contara, el ganador habría jugado 2 partidos con
        // PF=140/PC=160 en vez de PF=80/PC=70 — estas dos cifras concretas solo
        // cuadran si de verdad se excluyó.
        var filaGanador = ExtraerFila(adminHtml, equipoNombres[0]);
        Assert.Contains("<td>80</td>", filaGanador);
        Assert.Contains("<td>70</td>", filaGanador);

        // Igual para el perdedor: PF=70/PC=80 solo si no cuenta la segunda jornada
        // (si contara, sería PF=160/PC=140).
        var filaPerdedor = ExtraerFila(adminHtml, equipoNombres[1]);
        Assert.Contains("<td>70</td>", filaPerdedor);
        Assert.Contains("<td>80</td>", filaPerdedor);

        using var anonimo = fixture.CreateAnonymousWebHttpClient();
        var publicHtml = await (await anonimo.GetAsync($"/competiciones/{competicionId}/clasificacion", cancellationToken)).Content.ReadAsStringAsync(cancellationToken);

        Assert.Contains(equipoNombres[0], publicHtml);
        Assert.Contains(equipoNombres[1], publicHtml);
        Assert.True(publicHtml.IndexOf(equipoNombres[0], StringComparison.Ordinal) < publicHtml.IndexOf(equipoNombres[1], StringComparison.Ordinal));
    }

    [Fact]
    public async Task UnEquipoSinPartidosApareceEnLaTabla()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = fixture.CreateWebHttpClient();
        await LoginAsync(client, cancellationToken);

        var (competicionId, _, equipoNombres, _, _) = await CrearCompeticionConEquiposAsync(client, "clasificacion-sin-partidos", numeroDeEquipos: 1, cancellationToken);

        var html = await (await client.GetAsync($"/Admin/Clasificacion/{competicionId}", cancellationToken)).Content.ReadAsStringAsync(cancellationToken);

        var fila = ExtraerFila(html, equipoNombres[0]);
        Assert.Contains("<td>0</td>", fila);
    }

    [Fact]
    public async Task LaPaginaPublicaTieneEnlaceDeVueltaACompeticiones()
    {
        // BAS-18: la clasificación pública no tenía ningún enlace de vuelta, a
        // diferencia del resto de páginas del área pública.
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = fixture.CreateWebHttpClient();
        await LoginAsync(client, cancellationToken);

        var (competicionId, _, _, temporadaId, _) = await CrearCompeticionConEquiposAsync(client, "clasificacion-volver", numeroDeEquipos: 1, cancellationToken);

        using var anonimo = fixture.CreateAnonymousWebHttpClient();
        var html = await (await anonimo.GetAsync($"/competiciones/{competicionId}/clasificacion", cancellationToken)).Content.ReadAsStringAsync(cancellationToken);

        Assert.Contains($"""href="/temporadas/{temporadaId}/competiciones">Volver a competiciones</a>""", html);
    }

    [Fact]
    public async Task LaPaginaPublicaLlevaOutputCachingYLaAdminNo()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = fixture.CreateWebHttpClient();
        await LoginAsync(client, cancellationToken);

        var (competicionId, _, _, _, _) = await CrearCompeticionConEquiposAsync(client, "clasificacion-cache", numeroDeEquipos: 1, cancellationToken);

        using var anonimo = fixture.CreateAnonymousWebHttpClient();
        using var primeraPublica = await anonimo.GetAsync($"/competiciones/{competicionId}/clasificacion", cancellationToken);
        using var segundaPublica = await anonimo.GetAsync($"/competiciones/{competicionId}/clasificacion", cancellationToken);

        // La segunda petición dentro del TTL se sirve desde el Output Cache de
        // ASP.NET Core, que añade la cabecera Age a las respuestas servidas desde
        // caché (RFC 7234) — la primera, todavía sin entrada en caché, no la lleva.
        Assert.False(primeraPublica.Headers.Contains("Age"));
        Assert.True(segundaPublica.Headers.Contains("Age"));

        using var admin = await client.GetAsync($"/Admin/Clasificacion/{competicionId}", cancellationToken);
        Assert.False(admin.Headers.Contains("Age"));
    }
}
