---
codigo: BAS-8
estado: Planificado
tags:
  - tasks
---

# BAS-8: Tareas

- [x] Añadir `docs/specs/BAS-8/` a `BasketBaseTracker.slnx`.
- [ ] Añadir `[ValidateNever]` a `FichaJugador.Equipo`.
- [ ] Scaffold de `FichaJugador` en `Areas/Admin/Pages/FichaJugador/` (`dotnet aspnet-codegenerator razorpage`), incluido `Delete` esta vez (no se borra).
- [ ] Reescribir `Index.cshtml`/`.cs` con ruta `{equipoId:int}`, cabecera con el nombre del equipo, listado ordenado por dorsal.
- [ ] Reescribir `Create.cshtml`/`.cs` con ruta `{equipoId:int}`, desplegable de `Posicion` con opción en blanco.
- [ ] Reescribir `Edit.cshtml`/`.cs`, resolviendo el `equipoId` de vuelta desde la ficha cargada.
- [ ] Reescribir `Delete.cshtml`/`.cs` (confirmación explícita antes de borrar).
- [ ] Capturar `DbUpdateException` (restricción única de dorsal) en `Create`/`Edit`, con mensaje en español.
- [ ] Añadir enlace "Plantilla" por fila en `Equipo/Index.cshtml` (BAS-7).
- [ ] Test de integración de alta + edición + baja + listado para `FichaJugador`.
- [ ] Test de integración que confirma que un dorsal duplicado en el mismo equipo falla con un error de validación, no con un 500.
- [ ] Verificación manual (HTTP directo o navegador según disponibilidad): alta, edición, baja con confirmación, forzar el error de dorsal duplicado.
- [ ] `dotnet build` y `dotnet test` en verde en local antes de empujar (incluido el proyecto E2E).
- [ ] Confirmar CI (build + tests) en verde en el PR.
- [ ] Cierre: mover `docs/specs/BAS-8/` a `docs/specs/archive/BAS-8/`, abrir el PR de `feature/BAS-8` a `develop` y esperar confirmación humana para fusionarlo.
