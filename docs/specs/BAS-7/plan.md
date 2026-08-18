---
codigo: BAS-7
estado: Planificado
tags:
  - plan
---

# BAS-7: Plan técnico

## Entidades del modelo de datos afectadas

`Competicion`, `Equipo` (BAS-5) — sin cambios de esquema, solo pantallas sobre las entidades ya existentes.

## Pantallas afectadas

De `screens.md`, sección "Área admin" → "Gestión anual": `Competiciones`, `Equipos`.

## Decisiones técnicas específicas de este incremento

### Scaffolding y desplegables

Mismo punto de partida que BAS-6 (`dotnet aspnet-codegenerator razorpage`), con más revisión manual porque estas dos entidades sí tienen claves foráneas:

- **`Competicion`**: el scaffolding genera un `SelectList` por cada FK (`TemporadaId`, `CategoriaId`) usando la primera propiedad `string` de la entidad relacionada como texto — correcto por accidente para `Temporada.Nombre`, pero hay que revisar que también lo sea para `Categoria` (`Nombre`, no `Orden`). Ordenados igual que en sus propios listados de BAS-6 (`Temporada` por `FechaInicio` descendente, `Categoria` por `Orden`).
- **`Equipo`**: `ClubId` y `SedeHabitualId` se resuelven igual (texto = `Nombre`, `SedeHabitualId` con una opción en blanco al ser nullable). `CompeticionId` no tiene una propiedad `string` propia de la que tirar — el desplegable se construye a mano en el `PageModel`, proyectando `Temporada.Nombre + " — " + Categoria.Nombre` como texto de cada opción:

  ```csharp
  var competiciones = await context.Competiciones
      .Include(c => c.Temporada)
      .Include(c => c.Categoria)
      .OrderByDescending(c => c.Temporada.FechaInicio)
      .Select(c => new { c.Id, Texto = c.Temporada.Nombre + " — " + c.Categoria.Nombre })
      .ToListAsync();
  ViewData["CompeticionId"] = new SelectList(competiciones, "Id", "Texto", Equipo.CompeticionId);
  ```

### Restricción única de `Competicion` en la UI

`Create`/`Edit` de `Competicion` envuelven `SaveChangesAsync` en un `try/catch` de `DbUpdateException`, comprobando si la excepción interna es una violación de índice único de SQL Server (número de error 2601/2627) antes de añadir un `ModelState.AddModelError` en español ("Ya existe una competición para esa temporada y categoría.") y volver a `Page()`. No se valida por adelantado con una consulta separada (`AnyAsync` antes de guardar) porque eso abre una ventana de carrera entre la comprobación y el guardado — más simple y más correcto capturar el error real de la base de datos, que ya tiene la restricción como fuente de verdad (BAS-5).

### Concurrencia optimista de `Equipo`

`Edit.cshtml` incluye `<input type="hidden" asp-for="Equipo.RowVersion" />` (el `<form>` ya lo envía en cada `POST`, el usuario nunca lo ve ni lo edita). `Edit.cshtml.cs` envuelve `SaveChangesAsync` en `try/catch` de `DbUpdateConcurrencyException` y añade un `ModelState.AddModelError` en español ("Este equipo se ha modificado en otro sitio mientras tanto. Recarga la página e inténtalo de nuevo.") antes de volver a `Page()` — sin intentar una fusión automática de cambios (fuera del alcance que pide `data-model.md`, que solo exige evitar la sobrescritura silenciosa, no resolverla).

### Sin capa de ViewModel/DTO

Igual que BAS-6: las páginas enlazan directamente contra `Data/Entities/Competicion.cs` y `Equipo.cs`. Los desplegables van en `ViewData`, no en propiedades nuevas de la entidad.

### Verificación

Test de integración por entidad (`tests/BasketBaseTracker.Tests/Integration/`), reutilizando `AppHostSqlFixture` (BAS-5/BAS-6): alta con datos de catálogo ya creados dentro del propio test (una `Temporada`, una `Categoria`, opcionalmente un `Club`/`Sede`), edición, listado. Más un test específico que da de alta dos `Competicion` con la misma temporada+categoría y comprueba que la segunda falla con el mensaje de validación esperado, no con una excepción sin capturar (`response.StatusCode` sigue siendo 200, con el error en el HTML).
