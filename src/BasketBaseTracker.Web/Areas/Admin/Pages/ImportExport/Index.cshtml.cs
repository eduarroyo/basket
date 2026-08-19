using System.Text;
using System.Text.Json;
using Azure.Storage.Blobs;
using BasketBaseTracker.Web.Data;
using BasketBaseTracker.Web.Data.ImportExport;
using BasketBaseTracker.Web.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BasketBaseTracker.Web.Areas.Admin.Pages.ImportExport;

// Exportación e importación completa de todos los datos del sistema
// (architecture.md punto 10) — pantalla accesible solo al rol "Administrador"
// (sistema), configurado como excepción a "GestorCompeticion" en Program.cs.
public class IndexModel(ApplicationDbContext context, BlobServiceClient blobServiceClient) : PageModel
{
    private const string NombreContenedorBackups = "backups-importacion";

    private static readonly JsonSerializerOptions OpcionesJson = new() { WriteIndented = true };

    [BindProperty]
    public IFormFile? Fichero { get; set; }

    [BindProperty]
    public bool ConfirmoReemplazo { get; set; }

    public IReadOnlyList<string> Errores { get; set; } = [];

    public string? MensajeExito { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnGetExportarAsync(CancellationToken cancellationToken)
    {
        var datos = await CargarExportacionAsync(cancellationToken);
        var json = SerializarJson(datos);
        var nombreFichero = $"basketbasetracker-export-{DateTime.UtcNow:yyyyMMddHHmmss}.json";
        return File(Encoding.UTF8.GetBytes(json), "application/json", nombreFichero);
    }

    public async Task<IActionResult> OnPostImportarAsync(CancellationToken cancellationToken)
    {
        if (Fichero is null || Fichero.Length == 0)
        {
            Errores = ["Selecciona un fichero JSON de exportación."];
            return Page();
        }

        if (!ConfirmoReemplazo)
        {
            Errores = ["Debes confirmar que entiendes que esto reemplaza todos los datos actuales antes de continuar."];
            return Page();
        }

        ExportacionDatos datos;
        try
        {
            await using var stream = Fichero.OpenReadStream();
            datos = await JsonSerializer.DeserializeAsync<ExportacionDatos>(stream, OpcionesJson, cancellationToken)
                ?? throw new JsonException("El fichero está vacío o no tiene el formato esperado.");
        }
        catch (JsonException ex)
        {
            Errores = [$"El fichero no es un JSON válido: {ex.Message}"];
            return Page();
        }

        var erroresValidacion = ImportExportService.Validar(datos);
        if (erroresValidacion.Count > 0)
        {
            Errores = erroresValidacion;
            return Page();
        }

        // Backup automático del estado actual antes de reemplazar nada — condición
        // previa obligatoria (architecture.md punto 10, ampliado en spec.md): si
        // falla la subida, no se llega a importar.
        var estadoActual = await CargarExportacionAsync(cancellationToken);
        var nombreBackup = $"backup-{DateTime.UtcNow:yyyyMMddHHmmss}.json";
        var contenedor = blobServiceClient.GetBlobContainerClient(NombreContenedorBackups);
        await contenedor.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        await using (var streamBackup = new MemoryStream(Encoding.UTF8.GetBytes(SerializarJson(estadoActual))))
        {
            await contenedor.UploadBlobAsync(nombreBackup, streamBackup, cancellationToken: cancellationToken);
        }

        await ImportadorDatos.ImportarAsync(context, datos, cancellationToken);

        MensajeExito = $"Importación aplicada correctamente. Backup del estado anterior guardado en Blob Storage como '{nombreBackup}'.";
        return Page();
    }

    private async Task<ExportacionDatos> CargarExportacionAsync(CancellationToken cancellationToken) =>
        ImportExportService.Exportar(
            await context.Sedes.AsNoTracking().ToListAsync(cancellationToken),
            await context.Clubes.AsNoTracking().ToListAsync(cancellationToken),
            await context.Temporadas.AsNoTracking().ToListAsync(cancellationToken),
            await context.Categorias.AsNoTracking().ToListAsync(cancellationToken),
            await context.Competiciones.AsNoTracking().ToListAsync(cancellationToken),
            await context.Equipos.AsNoTracking().ToListAsync(cancellationToken),
            await context.FichasJugador.AsNoTracking().ToListAsync(cancellationToken),
            await context.Jornadas.AsNoTracking().ToListAsync(cancellationToken),
            await context.Partidos.AsNoTracking().ToListAsync(cancellationToken),
            await context.PartidosParciales.AsNoTracking().ToListAsync(cancellationToken),
            await context.PenalizacionesClasificacion.AsNoTracking().ToListAsync(cancellationToken));

    private static string SerializarJson(ExportacionDatos datos) => JsonSerializer.Serialize(datos, OpcionesJson);
}
