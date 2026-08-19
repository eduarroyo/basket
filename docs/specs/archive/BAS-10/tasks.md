---
codigo: BAS-10
estado: Completado
tags:
  - tasks
---

# BAS-10: Tareas

- [x] Crear `src/BasketBaseTracker.Web/Domain/ResultadoReglas.cs` (`RequiereMarcador`, `MarcadorValido`, `ResolucionValida`, `MarcadorTecnicoSugerido`).
- [x] `tests/BasketBaseTracker.Tests/Unit/ResultadoReglasTests.cs` con los casos descritos en `plan.md`.
- [x] Añadir restricción única `(PartidoId, NumeroPeriodo)` a `PartidoParcialConfiguration.cs` + migración EF Core.
- [x] Añadir `[Required]`/`[StringLength]`/`[Display]`/`[ValidateNever]` donde falten en `Partido.cs` (campos de resultado) y `PartidoParcial.cs`.
- [x] `Areas/Admin/Pages/Resultado/Edit.cshtml(.cs)`: Estado, marcador, motivo/ganador (visibles solo si aplica), Observaciones, RowVersion oculto, patrón "cargar y parchear" + verificación de concurrencia (`OriginalValue`), script de sugerencia de marcador técnico.
- [x] Scaffolding + reescritura de `Areas/Admin/Pages/PartidoParcial/{Index,Create,Edit,Delete}.cshtml(.cs)` (rutas anidadas por `partidoId`/`id`, restricción única, página de confirmación de baja).
- [x] Columna `Estado`/marcador + enlace "Resultado" en `Areas/Admin/Pages/Partido/Index.cshtml`; enlace "Parciales" desde `Resultado/Edit.cshtml`.
- [x] Tests de integración HTTP (`tests/BasketBaseTracker.Tests/Integration/ResultadoAdminPagesTests.cs`): marcar jugado, resolver administrativamente, revertir a programado (limpia marcador/motivo/ganador), rechazo de empate, rechazo de resolución sin motivo/ganador o con ganador ajeno al partido, conflicto de concurrencia.
- [x] Tests de integración HTTP (`tests/BasketBaseTracker.Tests/Integration/PartidoParcialAdminPagesTests.cs`): alta, edición, baja con confirmación, rechazo de periodo duplicado.
- [x] Añadir `docs/specs/BAS-10/` a `BasketBaseTracker.slnx`.
- [~] Verificación manual en navegador (Claude in Chrome): la extensión no estaba conectada en esta sesión — cubierto en su lugar por los tests de integración HTTP, que ejercitan las mismas páginas end-to-end. Pendiente una verificación visual manual cuando la extensión esté disponible.
- [x] `dotnet build` + `dotnet test` en verde localmente (53/53, incluye E2E), tras arreglar un bug latente de decodificación HTML en la extracción de `RowVersion` de los tests de integración (ver `plan.md`).
- [x] Actualizar `spec.md` (estado `Completado`, checklist de criterios de aceptación) y mover la carpeta a `docs/specs/archive/BAS-10/`.
- [x] Commit, push, PR de `feature/BAS-10` a `develop`; esperar confirmación humana antes de fusionar.
