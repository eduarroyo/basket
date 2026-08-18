---
codigo: BAS-12
estado: Completado
tags:
  - tasks
---

# BAS-12: Tareas

- [x] `Areas/Public/Pages/Index.cshtml(.cs)` (Portada): listado de temporadas, la `EnCurso` resaltada, enlace a cada listado de competiciones.
- [x] `Areas/Public/Pages/Temporadas/Competiciones.cshtml(.cs)`, ruta `/temporadas/{id}/competiciones`: competiciones agrupadas por categoría, equipos anidados con enlace a su ficha, enlace a la clasificación de cada competición.
- [x] `Areas/Public/Pages/Equipos/Index.cshtml(.cs)`, ruta `/equipos/{id}`: datos del equipo, enlaces a club/sede habitual, plantilla completa.
- [x] `Areas/Public/Pages/Clubes/Index.cshtml(.cs)`, ruta `/clubes/{id}`: datos del club, equipos de la temporada `EnCurso` con enlace a su ficha.
- [x] `Areas/Public/Pages/Sedes/Index.cshtml(.cs)`, ruta `/sedes/{id}`: datos de la sede.
- [x] `[OutputCache(PolicyName = "Publico")]` en las cinco páginas.
- [x] `NotFound()` en `id` inexistente en las cinco páginas.
- [x] Tests de integración HTTP (`tests/BasketBaseTracker.Tests/Integration/PublicPagesTests.cs`): contenido, navegación, `Ficha de club` sin temporada `EnCurso`, 404 en `id` inexistente, Output Caching activo en las cinco.
- [x] Añadir `docs/specs/BAS-12/` a `BasketBaseTracker.slnx`.
- [~] Verificación manual en navegador (Claude in Chrome): la extensión sí estaba conectada esta vez, pero `aspire run` local no llegó a levantar `Web` — la contraseña de `sa` del volumen de datos persistente de SQL Server no coincide con la generada para esta sesión (hallazgo operativo documentado en `plan.md`, no relacionado con este incremento). Cubierto en su lugar por los tests de integración HTTP, que ejercitan las mismas páginas end-to-end contra SQL efímero (no afectado por el problema del volumen).
- [x] `dotnet build` + `dotnet test` en verde localmente (69/69, incluye E2E).
- [x] Actualizar `spec.md` (estado `Completado`, checklist de criterios de aceptación) y mover la carpeta a `docs/specs/archive/BAS-12/`.
- [ ] Commit, push, PR de `feature/BAS-12` a `develop`; esperar confirmación humana antes de fusionar.
