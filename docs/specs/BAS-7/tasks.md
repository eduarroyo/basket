---
codigo: BAS-7
estado: Planificado
tags:
  - tasks
---

# BAS-7: Tareas

- [x] Añadir `docs/specs/BAS-7/` a `BasketBaseTracker.slnx`.
- [x] Scaffold de `Competicion` en `Areas/Admin/Pages/Competicion/` (`dotnet aspnet-codegenerator razorpage`); borrar `Details`/`Delete`.
- [x] Scaffold de `Equipo` en `Areas/Admin/Pages/Equipo/` (`dotnet aspnet-codegenerator razorpage`); borrar `Details`/`Delete`.
- [x] Revisar los desplegables generados de `Competicion` (Temporada, Categoría) — orden y texto correctos.
- [x] Construir a mano el desplegable de `CompeticionId` en `Equipo` (Temporada + Categoría combinadas) y revisar los de `ClubId`/`SedeHabitualId`.
- [x] Añadir `[ValidateNever]` a las propiedades de navegación de `Competicion`/`Equipo` — sin esto, `ModelState.IsValid` era siempre falso en alta/edición por la validación implícita de *nullable reference types* (ver `plan.md`).
- [x] Capturar `DbUpdateException` (restricción única) en `Competicion/Create` y `Competicion/Edit`, con mensaje en español.
- [x] Añadir `RowVersion` oculto en `Equipo/Edit.cshtml` y capturar `DbUpdateConcurrencyException` en `Equipo/Edit.cshtml.cs`, con mensaje en español.
- [x] Añadir enlaces a `Competiciones` y `Equipos` en la navegación del área Admin (`Areas/Admin/Pages/Shared/_Layout.cshtml`, `Index.cshtml`).
- [x] Test de integración de alta + edición + listado para `Competicion`.
- [x] Test de integración de alta + edición + listado (con conflicto de concurrencia) para `Equipo`.
- [x] Test de integración que confirma que dar de alta una `Competicion` duplicada (misma temporada+categoría) falla con un error de validación, no con un 500.
- [x] Verificación manual: HTTP directo (login, alta, listados, mensajes de error) — la extensión del navegador no estaba disponible en el momento de verificar; cubierto en su lugar con inspección directa del HTML devuelto durante la depuración de los tests.
- [x] `dotnet build` y `dotnet test` en verde en local antes de empujar (incluido el proyecto E2E).
- [ ] Confirmar CI (build + tests) en verde en el PR.
- [ ] Cierre: mover `docs/specs/BAS-7/` a `docs/specs/archive/BAS-7/`, abrir el PR de `feature/BAS-7` a `develop` y esperar confirmación humana para fusionarlo.
