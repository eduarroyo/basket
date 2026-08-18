---
codigo: BAS-8
estado: Archivado
tags:
  - plan
---

# BAS-8: Plan técnico

## Entidades del modelo de datos afectadas

`FichaJugador` (BAS-5) — sin cambios de esquema, solo pantallas sobre la entidad ya existente.

## Pantallas afectadas

De `screens.md`, sección "Área admin" → "Gestión anual": `Plantilla de equipo`.

## Decisiones técnicas específicas de este incremento

### Rutas ancladas al equipo, no a la ficha

Carpeta `Areas/Admin/Pages/FichaJugador/`, pero con `equipoId` como parte de la ruta en `Index`/`Create` (no un `[FromQuery]`, para que la URL sea limpia y compartible):

- `Index.cshtml` — `@page "{equipoId:int}"`, ruta `/Admin/FichaJugador/{equipoId}`. Carga el `Equipo` (para el nombre de cabecera) y sus `FichaJugador`, ordenadas por `Dorsal`.
- `Create.cshtml` — `@page "{equipoId:int}"`, ruta `/Admin/FichaJugador/Create/{equipoId}`.
- `Edit.cshtml`/`Delete.cshtml` — `@page "{id:int}"`, igual que el resto de la app; `EquipoId` viaja como parte de la ficha cargada, no hace falta en la ruta. El enlace "Volver a la plantilla" de ambas resuelve el `equipoId` a partir de la ficha ya cargada, no de un parámetro de ruta propio.

`Equipo/Index.cshtml` (BAS-7) añade un enlace "Plantilla" por fila, apuntando a `/Admin/FichaJugador/{equipoId}`.

### Baja con confirmación (`Delete.cshtml`)

Único page-set de la app hasta ahora que necesita `Delete`: se mantiene el patrón por defecto del scaffolding (`OnGetAsync` carga y muestra la ficha sin borrar nada; `OnPostAsync` borra) en vez de un único botón "Baja" sin confirmación en el listado — un borrado es irreversible y merece un paso explícito, coherente con las guías de seguridad del propio asistente para acciones destructivas.

### Restricción única `(EquipoId, Dorsal)` en la UI

Mismo patrón que la restricción única de `Competicion` (BAS-7): `Create`/`Edit` capturan `DbUpdateException` por violación de índice único (SQL Server 2601/2627) y añaden un `ModelState.AddModelError` en español ("Ya existe un jugador con ese dorsal en este equipo.") en vez de dejar propagar una excepción sin capturar.

### `Posicion` nullable

Desplegable con una opción en blanco ("Sin posición asignada"), igual que `SedeHabitualId` de `Equipo` (BAS-7): `<option value="">...</option>` manual antes de `asp-items="Html.GetEnumSelectList<Posicion>()"`.

### `[ValidateNever]` en `FichaJugador.Equipo`

Mismo bug de BAS-7 (propiedad de navegación no *nullable* implícitamente obligatoria por *nullable reference types*) — se aplica `[ValidateNever]` desde el principio en vez de descubrirlo otra vez por un test que falla en silencio.

### `[Display]` en los valores del enum `Posicion`

`AlaPivot`/`Pivot` (nombres de miembro de C#, sin tildes ni guion por restricciones del lenguaje) se muestran en el desplegable como "Ala-Pívot"/"Pívot" — el texto exacto de `data-model.md` — con `[Display(Name = "...")]` en cada valor del enum. `Html.GetEnumSelectList<T>()` ya lo respeta de fábrica; sin el atributo se mostraba el nombre C# tal cual ("AlaPivot"), detectado en la verificación manual en navegador.

### Verificación

Test de integración (`tests/BasketBaseTracker.Tests/Integration/PlantillaAdminPagesTests.cs`), reutilizando `AppHostSqlFixture` y `AdminHttpTestHelpers` (BAS-6/BAS-7, incluida ahora también `ExtraerId` — promovida de `GestionAnualAdminPagesTests` al helper compartido en vez de duplicarla de nuevo): crea el catálogo y el equipo necesarios dentro del propio test (`Temporada`, `Categoria`, `Club`, `Competicion`, `Equipo`), luego alta + edición + baja + listado de `FichaJugador`, más un test que da de alta dos fichas con el mismo dorsal en el mismo equipo y comprueba el mensaje de error. Misma cautela que en BAS-7 con la extracción de IDs (acotada a la fila, no al primer enlace de la página) y con no comparar texto acentuado contra HTML crudo en las aserciones — el mensaje de dorsal duplicado se dejó deliberadamente sin tildes para evitar el problema.

**Antifalsificación también en el POST de baja**: a diferencia de `Create`/`Edit`, el formulario de `Delete.cshtml` no tiene ningún campo visible más allá del botón — es fácil olvidar que el *tag helper* de Razor sigue inyectando el token oculto igualmente, y que el test tiene que extraerlo de la página de confirmación (`GetAntiforgeryTokenAsync`) antes del `POST`. Detectado por un `400` en el primer intento del test, no por ningún fallo del propio `Delete.cshtml.cs`.
