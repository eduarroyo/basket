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

### `[ValidateNever]` en las propiedades de navegación — primer bug real de este incremento

`Competicion.Temporada`/`Categoria` y `Equipo.Competicion`/`Club` son las primeras propiedades de navegación no *nullable* que aparecen en un formulario de Admin (BAS-6 no tenía ninguna FK). Sin `[ValidateNever]` (`Microsoft.AspNetCore.Mvc.ModelBinding.Validation`), ASP.NET Core las trata como implícitamente obligatorias por ser tipos de referencia no *nullable* (*nullable reference types* del proyecto) — el formulario nunca las rellena (solo envía `TemporadaId`/`CategoriaId` etc.), así que `ModelState.IsValid` era `false` en **todo** alta/edición, sin ningún mensaje visible que lo explicara: es un error a nivel de propiedad, no de modelo, y `asp-validation-summary="ModelOnly"` solo muestra errores de modelo. Detectado porque los tests de integración fallaban con "sigue en la página Create" sin ningún error legible — hubo que cambiar temporalmente `ModelOnly` por `All` en la vista para ver el mensaje real (`The Temporada field is required.`) y entender la causa. `Equipo.SedeHabitual` no necesita el atributo por ser *nullable* (`Sede?`).

### Verificación

Test de integración por entidad (`tests/BasketBaseTracker.Tests/Integration/GestionAnualAdminPagesTests.cs`), reutilizando `AppHostSqlFixture` (BAS-5/BAS-6): alta con datos de catálogo ya creados dentro del propio test (una `Temporada`, una `Categoria`, opcionalmente un `Club`), edición, listado. Más un test específico que da de alta dos `Competicion` con la misma temporada+categoría y comprueba que la segunda falla con el mensaje de validación esperado, no con una excepción sin capturar (`response.StatusCode` sigue siendo 200, con el error en el HTML); y un test de concurrencia que simula dos administradores editando el mismo `Equipo` a la vez (dos cargas del formulario, el segundo guardado usa un `RowVersion` ya obsoleto).

**Dos trampas de las propias pruebas, no del producto** (ambas descubiertas por fallos intermitentes/reproducibles al ejecutar la clase completa, no en aislamiento):

- **Extracción de IDs no acotada a la fila correcta**: los `[Fact]` de esta clase comparten la misma `IClassFixture<AppHostSqlFixture>` (misma base de datos), y xUnit los ejecuta en paralelo por defecto — el listado que un test lee puede contener filas creadas por otro test concurrente. Buscar "el primer enlace `/Admin/Entidad/Edit/N` de toda la página" después de crear un registro es ambiguo en ese escenario. Corregido con un `ExtraerId` que acota la búsqueda del enlace de edición a la misma fila `<tr>` que contiene el texto de referencia (nombre recién creado), no a la página entera.
- **Comparar contra texto acentuado en HTML crudo**: Razor escapa por defecto los caracteres no ASCII como entidades HTML (`ó` → `&#xF3;`) en el HTML que se envía por red — un `Assert.Contains("...categoría.", html)` sobre el cuerpo crudo de la respuesta nunca encuentra la cadena, aunque el navegador la muestre bien (el DOM ya decodificado sí la tiene). Mismo principio que en BAS-6 (que ya evitaba tildes en los datos de prueba), aplicado aquí también a los mensajes de error del propio código de producción: las aserciones comprueban una subcadena sin tildes (p. ej. `"Ya existe una competici"`) en vez del mensaje completo.
