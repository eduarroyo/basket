---
codigo: BAS-16
estado: Completado
tags:
  - plan
---

# BAS-16: Plan técnico

## Entidades del modelo de datos afectadas

Lectura/escritura completa de todas las entidades catálogo y transaccionales de `data-model.md`: `Sede`, `Club`, `Temporada`, `Categoria`, `Competicion`, `Equipo`, `FichaJugador`, `Jornada`, `Partido`, `PartidoParcial`, `PenalizacionClasificacion`. Sin cambios de esquema en ninguna de ellas.

`AspNetRoles`/`AspNetUserRoles` (Identity) — sin cambio de esquema, pero sí de **datos**: renombrado del rol `Administrador` a `GestorCompeticion` y alta del rol nuevo `Administrador`, vía migración idempotente del *seed* (ver más abajo), no vía migración EF Core (es un cambio de datos, no de esquema).

## Pantallas afectadas

- `Importación/exportación` (`screens.md`) — nueva, sustituye el *placeholder*. Una sola página en `Areas/Admin/Pages/ImportExport/Index.cshtml`, con dos acciones: exportar (`OnGetExportAsync`, descarga directa) e importar (`OnPostImportAsync`, subida de fichero + confirmación).
- Ninguna otra pantalla cambia de contenido, pero **todas las páginas actuales del área Admin cambian de rol exigido** (`Administrador` → `GestorCompeticion`).

## Decisiones técnicas específicas de este incremento

### División de roles

- `Program.cs`: la política `Administrador` pasa a `RequireRole("Administrador")` para el sistema (import/export) y se añade una política nueva `GestorCompeticion` con `RequireRole("GestorCompeticion")`.
- `AuthorizeAreaFolder("Admin", "/", "GestorCompeticion")` para todo el área Admin (login/logout siguen anónimos como hoy); la página `ImportExport` se marca aparte con `[Authorize(Policy = "Administrador")]` explícito en su `PageModel` (excepción a la convención de carpeta, igual de explícita que hoy lo son `AllowAnonymousToAreaPage` para Login/Logout).
- `IdentitySeeder` (renombrado conceptualmente, misma clase): añade un paso `MigrarRolesAsync` antes del *seed* del primer administrador, idempotente:
  1. Si existe el rol `Administrador` y **no** existe `GestorCompeticion`: renombra el rol en sitio (`RoleManager.UpdateAsync` sobre el mismo `IdentityRole`, cambiando `Name`/`NormalizedName`) — conserva `AspNetUserRoles` intacto, ningún usuario pierde su rol actual.
  2. Si no existe el rol `Administrador` (tras el paso anterior, o en una base de datos nueva): lo crea.
  3. Si tras la migración algún usuario tiene `GestorCompeticion` pero no `Administrador` **y es el único administrador existente** (caso de la cuenta ya sembrada): se le añade también `Administrador` — un único administrador legado pasa a tener ambos roles, sin credenciales nuevas.
  4. El *seed* del primer administrador (si no existe ninguno) sigue igual que hoy, pero asigna **ambos roles** a la cuenta nueva.
- Se aplica sola en el próximo despliegue: el *seed* corre en cada arranque de contenedor (`architecture.md` punto 7), así que la migración de roles de producción no necesita ningún paso manual — mismo pipeline ya usado en incrementos anteriores.
- Pruebas de integración existentes (`AdminHttpTestHelpers.LoginAsync` y todo lo que dependa de ella): sin cambios de comportamiento — la cuenta de prueba sembrada sigue teniendo acceso a todo el área Admin al recibir ambos roles.

### Formato del fichero de exportación

DTOs de exportación explícitos (no las entidades EF directamente, para desacoplar el formato de futuros cambios de esquema y evitar ciclos de navegación al serializar) en `Data/ImportExport/ExportacionDatos.cs` — un `record` por entidad con solo los campos escalares y FK, más el contenedor raíz:

```csharp
public record ExportacionDatos(
    int SchemaVersion,
    IReadOnlyList<SedeExport> Sedes,
    IReadOnlyList<ClubExport> Clubes,
    IReadOnlyList<TemporadaExport> Temporadas,
    IReadOnlyList<CategoriaExport> Categorias,
    IReadOnlyList<CompeticionExport> Competiciones,
    IReadOnlyList<EquipoExport> Equipos,
    IReadOnlyList<FichaJugadorExport> FichasJugador,
    IReadOnlyList<JornadaExport> Jornadas,
    IReadOnlyList<PartidoExport> Partidos,
    IReadOnlyList<PartidoParcialExport> PartidoParciales,
    IReadOnlyList<PenalizacionClasificacionExport> Penalizaciones);
```

`SchemaVersion` actual = `1` (constante `ImportExportService.SchemaVersionActual`). Serialización con `System.Text.Json` (ya en uso en el proyecto), formato indentado para que un fichero de backup sea inspeccionable a simple vista.

### `ImportExportService` (nuevo, `Domain/`)

Sin `DbContext` propio inyectado en los métodos puros — separa claramente:

- **`Exportar(...)`**: mapea listas de entidades ya cargadas (pasadas como parámetro, cargadas por la página con `context.Sedes.ToListAsync()` etc.) a `ExportacionDatos`. Función pura, testeable sin base de datos.
- **`Validar(ExportacionDatos datos)` → `IReadOnlyList<string>` (lista de errores, vacía si es válido)**: puro, sin base de datos.
  - `SchemaVersion` soportada (si no, un único error y no se comprueba nada más).
  - Integridad referencial: cada FK del fichero debe apuntar a un `Id` presente en la colección correspondiente del propio fichero (p. ej. `Equipo.CompeticionId` ∈ `Competiciones.Id`).
  - Restricciones únicas ya conocidas de `data-model.md`: `(TemporadaId, CategoriaId)` en `Competicion`, `(EquipoId, Dorsal)` en `FichaJugador`, `(CompeticionId, Numero)` en `Jornada`, `(PartidoId, NumeroPeriodo)` en `PartidoParcial`.
  - Invariante de negocio "un equipo no puede aparecer dos veces en la misma jornada" — reutiliza `PartidoReglas.EquipoYaJuegaEnJornada` (ya existe, BAS-9), sin duplicar la regla.
- **`ImportarAsync(ApplicationDbContext context, ExportacionDatos datos)`**: sí toca base de datos, solo se llama tras `Validar` sin errores. Dentro de una única transacción (`context.Database.BeginTransactionAsync`):
  1. Borra las entidades en alcance en orden inverso de dependencia (todas las FK son `DeleteBehavior.Restrict`, sin cascada — `Data/Configurations/`): `PenalizacionClasificacion`, `PartidoParcial`, `Partido`, `Jornada`, `FichaJugador`, `Equipo`, `Competicion`, y por último `Categoria`/`Club`/`Sede`/`Temporada` (sin dependencias entre sí).
  2. Inserta en orden de dependencia inverso al de borrado, **preservando el `Id` original de cada fila** (necesario para que un backup restaure enlaces externos ya existentes — feeds iCal de BAS-14, URLs `/partidos/{id}` ya compartidas): `SET IDENTITY_INSERT <tabla> ON` antes de cada `AddRange`+`SaveChangesAsync` de esa tabla, `OFF` justo después (SQL Server solo permite una tabla con IDENTITY_INSERT activo a la vez por conexión).
  3. `Commit` solo si todo el bloque anterior no lanza excepción; cualquier fallo revierte la transacción entera (ninguna tabla queda a medias).

### Backup automático antes de importar

Nuevo recurso `Azure Blob Storage` en `AppHost.cs` (`aspire add azure-storage`, mismo patrón que Key Vault — solo en modo publish, contenedor local en desarrollo vía la propia integración de Aspire). Antes del paso de borrado de `ImportarAsync`, la página exporta el estado actual (reutilizando `ImportExportService.Exportar`) y sube ese JSON a un contenedor `backups-importacion` con un nombre con marca de tiempo (`backup-{yyyyMMddHHmmss}.json`). Si la subida falla, no se procede con la importación (la salvaguarda es una condición previa, no un paso best-effort).

### Página `ImportExport`

- `OnGetExportAsync`: carga todas las entidades, llama a `ImportExportService.Exportar`, devuelve `File(...)` con `Content-Disposition: attachment` y el JSON serializado.
- Formulario de importación: `<input type="file">` + checkbox obligatorio "Entiendo que esto reemplaza todos los datos actuales" — validado en el mismo `OnPostImportAsync` (sin checkbox marcado, error de validación, nada se procesa). Evita necesitar un paso intermedio con estado persistido entre peticiones (TempData/sesión) solo para pedir una confirmación — más simple, cumple igualmente "confirmación explícita, no un único clic".
- `OnPostImportAsync`: parsea el JSON subido → `Validar` → si hay errores, los muestra sin tocar nada → si es válido, sube el backup a Blob Storage → `ImportarAsync` → mensaje de éxito con el nombre del backup subido.

### Tests

- **Unitario** (`tests/BasketBaseTracker.Tests/Unit/ImportExportServiceTests.cs`): `Exportar` mapea correctamente; `Validar` — versión no soportada (un único error), FK huérfana, restricción única violada, equipo repetido en jornada (reutilizando `PartidoReglas`), fichero válido sin errores.
- **Integración** (`tests/BasketBaseTracker.Tests/Integration/ImportExportAdminPagesTests.cs`): exportar y reimportar el mismo fichero dentro de la misma competición de prueba reproduce datos equivalentes (mismos `Id`, mismos campos); importar un fichero con versión no soportada no cambia nada; importar sin marcar el checkbox de confirmación no cambia nada; solo el rol `Administrador` accede a la página, `GestorCompeticion` solo (sin `Administrador`) recibe 403; el resto de páginas del área Admin siguen accesibles para `GestorCompeticion`.
- **Migración de roles** (`tests/BasketBaseTracker.Tests/Integration/`, o ampliando un test existente): arrancar el `AppHost` de test dos veces sobre el mismo volumen no aplica (los tests usan `Sql:Ephemeral`, sin volumen persistente entre ejecuciones) — se cubre en su lugar sembrando manualmente el rol legado `Administrador` con un usuario antes de invocar el *seed*, y comprobando que tras `MigrarRolesAsync` ese usuario tiene ambos roles y el rol `Administrador` original ya no existe con ese único usuario histórico (ha pasado a `GestorCompeticion`).
