---
codigo: BAS-14
estado: Completado
tags:
  - plan
---

# BAS-14: Plan técnico

## Entidades del modelo de datos afectadas

- `Partido` (lectura) — `FechaHora`, `Estado`, `EquipoLocalId`, `EquipoVisitanteId`, `SedeId`. Determina qué `VEVENT` se generan y su contenido.
- `Jornada` (lectura) — vía `Partido.JornadaId`, para filtrar los partidos de una `Competicion`.
- `Equipo` (lectura) — nombre para el resumen del evento; filtro del feed por equipo (local o visitante).
- `Sede` (lectura) — `Nombre`, `Municipio`, `Direccion` para el campo `LOCATION`.

Ningún cambio de esquema — este incremento solo lee datos ya existentes (ver `spec.md`, Fuera de alcance).

## Pantallas afectadas

- `Calendario de competición` (`/competiciones/{id}/calendario`, `Areas/Public/Pages/Competiciones/Calendario.cshtml`) — añadir enlace de suscripción a `calendario.ics`.
- `Ficha de equipo` (`/equipos/{id}`, `Areas/Public/Pages/Equipos/Index.cshtml`) — añadir enlace de suscripción a `calendario.ics`.
- `Suscripción iCal` (`screens.md`) — nuevas rutas `/competiciones/{id}/calendario.ics` y `/equipos/{id}/calendario.ics`, sin pantalla propia (devuelven el fichero, no HTML).

## Decisiones técnicas específicas de este incremento

### Implementación de las rutas `.ics`

Minimal API endpoints en `Program.cs` (no Razor Pages, porque no producen HTML) — `MapGet("/competiciones/{id:int}/calendario.ics", ...)` y `MapGet("/equipos/{id:int}/calendario.ics", ...)`, con `[OutputCache(PolicyName = "Publico")]` vía `.CacheOutput("Publico")` para reutilizar la misma política de TTL de 4 minutos que el resto del área pública (`architecture.md`, punto 5). Devuelven `Results.NotFound()` si el `id` no existe, o `Results.Text(ics, "text/calendar", Encoding.UTF8)` con el contenido generado.

### Generación del feed

Servicio de dominio `IcsFeedBuilder` en `Domain/` (siguiendo el patrón de `ClasificacionCalculator`/`ClasificacionService`: lógica pura, sin `DbContext`) con dos métodos:

- `ConstruirFeedCompeticion(Competicion competicion, IReadOnlyList<Partido> partidos)`
- `ConstruirFeedEquipo(Equipo equipo, IReadOnlyList<Partido> partidos)`

Ambos delegan en un método común que mapea `IReadOnlyList<Partido>` (ya cargados con `Include` de `EquipoLocal`, `EquipoVisitante`, `Sede`) a un `Calendar` de `Ical.Net` con un `CalendarEvent` por partido:

- **Filtro de partidos** (aplicado en la página de llamada, antes de pasar la lista al builder, igual que ya hace `Calendario.cshtml.cs`): `FechaHora != null && Estado != Estado.Cancelado`.
- `Uid`: `partido-{Partido.Id}@basketbasetracker.es` — estable entre regeneraciones del feed (mismo partido, mismo UID, requisito del criterio de aceptación).
- `Summary`: `"{EquipoLocal.Nombre} - {EquipoVisitante.Nombre}"`.
- `Start` / `DtStart`: `Partido.FechaHora` como `CalDateTime` en zona horaria `Europe/Madrid`.
- `Duration`: fija `TimeSpan.FromHours(2)` (ver `spec.md`, Aclaraciones — no hay hora de fin en el modelo).
- `Location`: `$"{Sede.Nombre}, {Sede.Municipio}"` si `Partido.SedeId` no es nulo (ni el de la sede habitual del equipo local si no se ha asignado explícitamente — se usa directamente `Partido.Sede`, que ya resuelve ese valor por defecto a nivel de datos); ausente si `Sede` es nula.
- Serialización con `CalendarSerializer` de `Ical.Net.Serialization` a texto `.ics`.

### Paquete NuGet

Añadir `Ical.Net` a `BasketBaseTracker.Web.csproj` con `dotnet add package` (skill `dotnet`) — decisión ya fijada en `architecture.md`, punto 9.

### Enlace de suscripción en las páginas existentes

Un enlace `<a href="...">` simple en `Calendario.cshtml` y `Equipos/Index.cshtml` (texto "Añadir a mi calendario", sin JavaScript ni detección de cliente) apuntando a la ruta `.ics` correspondiente con el `id` de la página actual. Sin icono ni componente nuevo — coherente con la sencillez del resto del área pública.

### Tests

- **Unitario** (`tests/BasketBaseTracker.Tests/Unit/`): `IcsFeedBuilder` — un partido con sede genera un `VEVENT` con los campos esperados (UID estable, resumen, inicio, duración, ubicación); un partido sin sede omite `LOCATION`; UID estable entre dos llamadas para el mismo partido.
- **Integración** (`tests/BasketBaseTracker.Tests/Integration/`, siguiendo el patrón de `CalendarioYResultadosPublicosTests.cs`): `GET /competiciones/{id}/calendario.ics` y `GET /equipos/{id}/calendario.ics` devuelven 200 con `Content-Type: text/calendar` y el número correcto de `VEVENT` (excluyendo cancelados y sin fecha); un `id` inexistente devuelve 404; el enlace de suscripción aparece en el HTML de `Calendario.cshtml` y `Equipos/Index.cshtml`.
- **Validación manual** (criterio de aceptación no automatizable): importar el feed generado en Google Calendar y confirmar que se muestra correctamente — se hace una vez, antes de cerrar el incremento, no como parte de la suite automatizada.
