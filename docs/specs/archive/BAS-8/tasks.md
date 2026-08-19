---
codigo: BAS-8
estado: Archivado
tags:
  - tasks
---

# BAS-8: Tareas

- [x] Añadir `docs/specs/BAS-8/` a `BasketBaseTracker.slnx`.
- [x] Añadir `[ValidateNever]` a `FichaJugador.Equipo`.
- [x] Scaffold de `FichaJugador` en `Areas/Admin/Pages/FichaJugador/` (`dotnet aspnet-codegenerator razorpage`), incluido `Delete` esta vez (no se borra).
- [x] Reescribir `Index.cshtml`/`.cs` con ruta `{equipoId:int}`, cabecera con el nombre del equipo, listado ordenado por dorsal.
- [x] Reescribir `Create.cshtml`/`.cs` con ruta `{equipoId:int}`, desplegable de `Posicion` con opción en blanco.
- [x] Reescribir `Edit.cshtml`/`.cs`, resolviendo el `equipoId` de vuelta desde la ficha cargada.
- [x] Reescribir `Delete.cshtml`/`.cs` (confirmación explícita antes de borrar).
- [x] Capturar `DbUpdateException` (restricción única de dorsal) en `Create`/`Edit`, con mensaje en español.
- [x] Añadir enlace "Plantilla" por fila en `Equipo/Index.cshtml` (BAS-7).
- [x] Añadir `[Display]` a `Posicion.AlaPivot`/`Pivot` ("Ala-Pívot"/"Pívot") — detectado al verificar en navegador.
- [x] Test de integración de alta + edición + baja + listado para `FichaJugador`.
- [x] Test de integración que confirma que un dorsal duplicado en el mismo equipo falla con un error de validación, no con un 500.
- [x] Verificación manual en navegador (Claude in Chrome): login, alta, dorsal duplicado, baja con confirmación — la extensión sí estaba disponible esta vez.
- [x] `dotnet build` y `dotnet test` en verde en local antes de empujar (incluido el proyecto E2E).
- [ ] Confirmar CI (build + tests) en verde en el PR.
- [ ] Cierre: mover `docs/specs/BAS-8/` a `docs/specs/archive/BAS-8/`, abrir el PR de `feature/BAS-8` a `develop` y esperar confirmación humana para fusionarlo.
