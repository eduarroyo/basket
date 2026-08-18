---
codigo: BAS-9
estado: Completado
tags:
  - tasks
---

# BAS-9: Tareas

- [x] Añadir `[ValidateNever]` y anotaciones (`[Required]`/`[StringLength]`/`[Display]`) a `Jornada.cs` y `Partido.cs`.
- [x] Crear `src/BasketBaseTracker.Web/Domain/PartidoReglas.cs` con `EquipoYaJuegaEnJornada`.
- [x] `tests/BasketBaseTracker.Tests/Unit/PartidoReglasTests.cs` con los casos descritos en `plan.md`.
- [x] Scaffolding + reescritura de `Areas/Admin/Pages/Jornada/{Index,Create,Edit}.cshtml(.cs)` (rutas anidadas por `competicionId`/`id`, validación de restricción única).
- [x] Scaffolding + reescritura de `Areas/Admin/Pages/Partido/{Index,Create,Edit}.cshtml(.cs)` (rutas anidadas por `jornadaId`/`id`, desplegables de equipo filtrados por competición, patrón "cargar y parchear" en Edit, validación de equipo repetido y equipo local == visitante).
- [x] Enlace "Calendario" en `Areas/Admin/Pages/Competicion/Index.cshtml` hacia `/Admin/Jornada/{competicionId}`; enlace "Partidos" en `Jornada/Index.cshtml` hacia `/Admin/Partido/{jornadaId}`.
- [x] Migración EF Core si las anotaciones nuevas cambian el modelo (`StringLength` de `Etiqueta`) — no hizo falta: `Etiqueta` ya tenía `HasMaxLength(200)` en `JornadaConfiguration` desde BAS-5, la anotación nueva coincide exactamente.
- [x] Tests de integración HTTP (`tests/BasketBaseTracker.Tests/Integration/CalendarioAdminPagesTests.cs`): alta + edición + listado de `Jornada` y `Partido`; rechazo de número de jornada duplicado; rechazo de equipo repetido en la jornada; rechazo de equipo local == visitante; edición de partido no toca `Estado`.
- [x] Añadir `docs/specs/BAS-9/` a `BasketBaseTracker.slnx`.
- [~] Verificación manual en navegador (Claude in Chrome): la extensión no estaba conectada en esta sesión, así que no se pudo hacer — cubierto en su lugar por los tests de integración HTTP de `CalendarioAdminPagesTests.cs`, que ejercitan las mismas páginas end-to-end (alta, edición, listado, mensajes de error en español). Pendiente una verificación visual manual antes o después de fusionar, cuando la extensión esté disponible.
- [x] `dotnet build` + `dotnet test` en verde localmente (29/29, incluye E2E).
- [x] Actualizar `spec.md` (estado `Completado`, checklist de criterios de aceptación) y mover la carpeta a `docs/specs/archive/BAS-9/`.
- [ ] Commit, push, PR de `feature/BAS-9` a `develop`; esperar confirmación humana antes de fusionar.
