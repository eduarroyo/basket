---
codigo: BAS-13
estado: Completado
tags:
  - tasks
---

# BAS-13: Tareas

- [x] `Areas/Public/Pages/Competiciones/Calendario.cshtml(.cs)`, ruta `/competiciones/{id}/calendario`: jornadas con sus partidos (local, visitante, sede, fecha/hora, estado), enlaces a `Resultados por jornada` y `Detalle de partido`.
- [x] `Areas/Public/Pages/Competiciones/Jornada.cshtml(.cs)`, ruta `/competiciones/{id}/jornadas/{n}`: partidos de esa jornada con marcador/estado, enlace a `Detalle de partido`.
- [x] `Areas/Public/Pages/Partidos/Index.cshtml(.cs)`, ruta `/partidos/{id}`: marcador, parciales, sede, estado, motivo/ganador si `Resuelto`, enlaces a equipos y sede.
- [x] `Areas/Public/Pages/Equipos/Resultados.cshtml(.cs)`, ruta `/equipos/{id}/resultados`: histórico de partidos del equipo, enlace a `Detalle de partido`.
- [x] Enlace "Calendario" en `Areas/Public/Pages/Temporadas/Competiciones.cshtml` (BAS-12); enlace "Resultados" en `Areas/Public/Pages/Equipos/Index.cshtml` (BAS-12).
- [x] `[OutputCache(PolicyName = "Publico")]` en las cuatro páginas.
- [x] `NotFound()` en `id`/`n` inexistente en las cuatro páginas.
- [x] Tests de integración HTTP (`tests/BasketBaseTracker.Tests/Integration/CalendarioYResultadosPublicosTests.cs`): contenido, navegación, 404, Output Caching activo en las cuatro.
- [x] Añadir `docs/specs/BAS-13/` a `BasketBaseTracker.slnx`.
- [~] Verificación manual en navegador (Claude in Chrome): la extensión no estaba conectada en esta sesión, y el entorno local de `aspire run` sigue con el problema de contraseña de `sa` desincronizada (hallazgo de BAS-12, sin resolver). Cubierto en su lugar por los tests de integración HTTP, que ejercitan las mismas páginas end-to-end contra SQL efímero.
- [x] `dotnet build` + `dotnet test` en verde localmente (74/74, incluye E2E).
- [x] Actualizar `spec.md` (estado `Completado`, checklist de criterios de aceptación) y mover la carpeta a `docs/specs/archive/BAS-13/`.
- [ ] Commit, push, PR de `feature/BAS-13` a `develop`; esperar confirmación humana antes de fusionar.
