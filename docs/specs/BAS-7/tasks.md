---
codigo: BAS-7
estado: Planificado
tags:
  - tasks
---

# BAS-7: Tareas

- [x] Añadir `docs/specs/BAS-7/` a `BasketBaseTracker.slnx`.
- [ ] Scaffold de `Competicion` en `Areas/Admin/Pages/Competicion/` (`dotnet aspnet-codegenerator razorpage`); borrar `Details`/`Delete`.
- [ ] Scaffold de `Equipo` en `Areas/Admin/Pages/Equipo/` (`dotnet aspnet-codegenerator razorpage`); borrar `Details`/`Delete`.
- [ ] Revisar los desplegables generados de `Competicion` (Temporada, Categoría) — orden y texto correctos.
- [ ] Construir a mano el desplegable de `CompeticionId` en `Equipo` (Temporada + Categoría combinadas) y revisar los de `ClubId`/`SedeHabitualId`.
- [ ] Capturar `DbUpdateException` (restricción única) en `Competicion/Create` y `Competicion/Edit`, con mensaje en español.
- [ ] Añadir `RowVersion` oculto en `Equipo/Edit.cshtml` y capturar `DbUpdateConcurrencyException` en `Equipo/Edit.cshtml.cs`, con mensaje en español.
- [ ] Añadir enlaces a `Competiciones` y `Equipos` en la navegación del área Admin (`Areas/Admin/Pages/Shared/_Layout.cshtml`, `Index.cshtml`).
- [ ] Test de integración de alta + edición + listado para `Competicion`.
- [ ] Test de integración de alta + edición + listado para `Equipo`.
- [ ] Test de integración que confirma que dar de alta una `Competicion` duplicada (misma temporada+categoría) falla con un error de validación, no con un 500.
- [ ] Verificación manual en `aspire run` (navegador): dar de alta una competición, un equipo, forzar el error de duplicado y el de concurrencia.
- [ ] `dotnet build` y `dotnet test` en verde en local antes de empujar (incluido el proyecto E2E).
- [ ] Confirmar CI (build + tests) en verde en el PR.
- [ ] Cierre: mover `docs/specs/BAS-7/` a `docs/specs/archive/BAS-7/`, abrir el PR de `feature/BAS-7` a `develop` y esperar confirmación humana para fusionarlo.
