---
codigo: BAS-6
estado: Archivado
tags:
  - tasks
---

# BAS-6: Tareas

- [x] Añadir `docs/specs/BAS-6/` a `BasketBaseTracker.slnx` (carpeta de solución, `workflow.md`).
- [x] `dotnet tool restore` — confirmar que `dotnet-aspnet-codegenerator` está disponible.
- [x] Scaffold de `Temporada`, `Categoria`, `Club`, `Sede` en `Areas/Admin/Pages/<Entidad>/` (`dotnet aspnet-codegenerator razorpage`). Requirió añadir `Microsoft.EntityFrameworkCore.Tools` (ausente hasta ahora) para que el scaffolding pudiera leer el modelo de EF Core.
- [x] Borrar `Details.cshtml(.cs)` y `Delete.cshtml(.cs)` de las cuatro carpetas (fuera de alcance).
- [x] Revisar el código generado: nombres de campos en español, validación acorde a `Data/Configurations/*Configuration.cs` — se añadieron `[Required]`/`[StringLength]`/`[Display]` a las entidades (el scaffolding no los genera) y se corrigió el desplegable de `Temporada.Estado` (`asp-items="Html.GetEnumSelectList<TemporadaEstado>()"`, vacío por defecto).
- [x] Crear `Areas/Admin/Pages/Shared/_Layout.cshtml` (navegación a las cuatro pantallas + cerrar sesión) y actualizar `Areas/Admin/Pages/_ViewStart.cshtml`.
- [x] Crear `Areas/Admin/Pages/Index.cshtml(.cs)` (página de inicio del área Admin con enlaces a las cuatro pantallas).
- [x] Cambiar el `returnUrl` por defecto de `Login.cshtml.cs` a `~/Admin`.
- [x] Test de integración de alta + edición + listado para `Temporada`.
- [x] Test de integración de alta + edición + listado para `Categoria`.
- [x] Test de integración de alta + edición + listado para `Club`.
- [x] Test de integración de alta + edición + listado para `Sede`.
- [x] Test de integración que confirma que una petición anónima a una página de catálogo redirige a `/Admin/Login`.
- [x] Verificación manual en `aspire run`: iniciar sesión, dar de alta y editar un registro de cada entidad desde el navegador (Claude in Chrome) — confirmado además que la validación cliente (`jquery.validate`) y servidor muestran los mensajes en español, y que cerrar sesión redirige a la web pública.
- [x] `dotnet build` y `dotnet test` en verde en local antes de empujar (12/12 tests).
- [x] Confirmar CI (build + tests) en verde en el PR.
- [x] Cierre: mover `docs/specs/BAS-6/` a `docs/specs/archive/BAS-6/`, abrir el PR de `feature/BAS-6` a `develop` y esperar confirmación humana para fusionarlo.
