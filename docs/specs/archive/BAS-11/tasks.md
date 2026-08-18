---
codigo: BAS-11
estado: Completado
tags:
  - tasks
---

# BAS-11: Tareas

- [x] Crear `src/BasketBaseTracker.Web/Domain/ClasificacionCalculator.cs` (`FilaClasificacion`, `Calcular`).
- [x] `tests/BasketBaseTracker.Tests/Unit/ClasificacionCalculatorTests.cs` con los casos descritos en `plan.md`.
- [x] Crear `src/BasketBaseTracker.Web/Domain/ClasificacionService.cs` (`FilaClasificacionVista`, `ObtenerAsync`) y registrarlo en `Program.cs`.
- [x] Configurar Output Caching en `Program.cs` (`AddOutputCache` con política `Publico` de 4 minutos, `UseOutputCache`).
- [x] `Areas/Public/Pages/Competiciones/Clasificacion.cshtml(.cs)`, ruta `/competiciones/{id}/clasificacion`, con `[OutputCache(PolicyName = "Publico")]`.
- [x] `Areas/Admin/Pages/Clasificacion/Index.cshtml(.cs)`, ruta `/Admin/Clasificacion/{competicionId}`, sin caché.
- [x] Enlace "Clasificación" en `Areas/Admin/Pages/Competicion/Index.cshtml`.
- [x] Tests de integración HTTP (`tests/BasketBaseTracker.Tests/Integration/ClasificacionPagesTests.cs`): tabla correcta en ambas páginas, jornada sin `CuentaParaClasificacion` excluida, cabecera de caché presente/ausente según corresponda (vía la cabecera `Age` en la segunda petición).
- [x] Añadir `docs/specs/BAS-11/` a `BasketBaseTracker.slnx`.
- [~] Verificación manual en navegador (Claude in Chrome): la extensión no estaba conectada en esta sesión — cubierto en su lugar por los tests de integración HTTP, que ejercitan las mismas páginas end-to-end. Pendiente una verificación visual manual cuando la extensión esté disponible.
- [x] `dotnet build` + `dotnet test` en verde localmente (61/61, incluye E2E).
- [x] Actualizar `spec.md` (estado `Completado`, checklist de criterios de aceptación) y mover la carpeta a `docs/specs/archive/BAS-11/`.
- [ ] Commit, push, PR de `feature/BAS-11` a `develop`; esperar confirmación humana antes de fusionar.
