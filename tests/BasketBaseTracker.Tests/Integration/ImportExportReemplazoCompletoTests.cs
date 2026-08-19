using System.Net;
using System.Text;
using System.Text.Json;
using BasketBaseTracker.Web.Data.ImportExport;
using static BasketBaseTracker.Tests.Integration.AdminHttpTestHelpers;

namespace BasketBaseTracker.Tests.Integration;

// Reemplazo completo real de BAS-16 — clase dedicada, sin otros [Fact]
// compartiendo la fixture: purga toda la base de datos, así que no puede
// convivir con otros tests que dependan de que sus propios datos sigan intactos
// si xUnit los ejecuta en paralelo (mismo motivo que MigracionRolesAdminTests).
public class ImportExportReemplazoCompletoTests(AppHostSqlFixture fixture) : IClassFixture<AppHostSqlFixture>
{
    private static readonly JsonSerializerOptions OpcionesJson = new() { WriteIndented = true };

    [Fact]
    public async Task ExportarYReimportarElMismoFicheroReproduceDatosEquivalentes()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = fixture.CreateWebHttpClient();
        await LoginAsync(client, cancellationToken);

        var (competicionId, equipoIds, equipoNombres, _, _) = await CrearCompeticionConEquiposAsync(client, "importexport-roundtrip", numeroDeEquipos: 2, cancellationToken);
        var jornadaId = await CrearJornadaAsync(client, competicionId, numero: 1, etiqueta: "Jornada 1 (importexport-roundtrip)", cancellationToken);
        await CrearPartidoAsync(client, jornadaId, equipoIds[0], equipoIds[1], equipoNombres[0], cancellationToken);

        var primeraExportacion = await client.GetAsync("/Admin/ImportExport?handler=Exportar", cancellationToken);
        primeraExportacion.EnsureSuccessStatusCode();
        var jsonOriginal = await primeraExportacion.Content.ReadAsStringAsync(cancellationToken);

        var editPage = await client.GetAsync("/Admin/ImportExport", cancellationToken);
        var token = await GetAntiforgeryTokenAsync(editPage, cancellationToken);

        using var content = new MultipartFormDataContent
        {
            { new StringContent(token), "__RequestVerificationToken" },
            { new StringContent("true"), "ConfirmoReemplazo" },
            { new ByteArrayContent(Encoding.UTF8.GetBytes(jsonOriginal)), "Fichero", "export.json" },
        };
        using var respuestaImport = await client.PostAsync("/Admin/ImportExport?handler=Importar", content, cancellationToken);
        var htmlImport = await respuestaImport.Content.ReadAsStringAsync(cancellationToken);

        Assert.Equal(HttpStatusCode.OK, respuestaImport.StatusCode);
        Assert.Contains("aplicada correctamente", htmlImport);

        var segundaExportacion = await client.GetAsync("/Admin/ImportExport?handler=Exportar", cancellationToken);
        var jsonReimportado = await segundaExportacion.Content.ReadAsStringAsync(cancellationToken);

        var datosOriginales = JsonSerializer.Deserialize<ExportacionDatos>(jsonOriginal, OpcionesJson)!;
        var datosReimportados = JsonSerializer.Deserialize<ExportacionDatos>(jsonReimportado, OpcionesJson)!;

        Assert.Equal(datosOriginales.Competiciones.Count, datosReimportados.Competiciones.Count);
        Assert.Equal(datosOriginales.Equipos, datosReimportados.Equipos);
        Assert.Equal(datosOriginales.Jornadas, datosReimportados.Jornadas);
        Assert.Equal(datosOriginales.Partidos, datosReimportados.Partidos);
    }
}
