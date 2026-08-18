---
codigo: BAS-6
estado: Planificado
tags:
  - tasks
---

# BAS-6: Tareas

- [x] Añadir `docs/specs/BAS-6/` a `BasketBaseTracker.slnx` (carpeta de solución, `workflow.md`).
- [ ] `dotnet tool restore` — confirmar que `dotnet-aspnet-codegenerator` está disponible.
- [ ] Scaffold de `Temporada`, `Categoria`, `Club`, `Sede` en `Areas/Admin/Pages/<Entidad>/` (`dotnet aspnet-codegenerator razorpage`).
- [ ] Borrar `Details.cshtml(.cs)` y `Delete.cshtml(.cs)` de las cuatro carpetas (fuera de alcance).
- [ ] Revisar el código generado: nombres de campos en español, validación acorde a `Data/Configurations/*Configuration.cs`.
- [ ] Crear `Areas/Admin/Pages/Shared/_Layout.cshtml` (navegación a las cuatro pantallas + cerrar sesión) y actualizar `Areas/Admin/Pages/_ViewStart.cshtml`.
- [ ] Crear `Areas/Admin/Pages/Index.cshtml(.cs)` (página de inicio del área Admin con enlaces a las cuatro pantallas).
- [ ] Cambiar el `returnUrl` por defecto de `Login.cshtml.cs` a `~/Admin`.
- [ ] Test de integración de alta + edición + listado para `Temporada`.
- [ ] Test de integración de alta + edición + listado para `Categoria`.
- [ ] Test de integración de alta + edición + listado para `Club`.
- [ ] Test de integración de alta + edición + listado para `Sede`.
- [ ] Test de integración que confirma que una petición anónima a una página de catálogo redirige a `/Admin/Login`.
- [ ] Verificación manual en `aspire run`: iniciar sesión, dar de alta y editar un registro de cada entidad desde el navegador.
- [ ] `dotnet build` y `dotnet test` en verde en local antes de empujar.
- [ ] Confirmar CI (build + tests) en verde en el PR.
- [ ] Cierre: mover `docs/specs/BAS-6/` a `docs/specs/archive/BAS-6/`, abrir el PR de `feature/BAS-6` a `develop` y esperar confirmación humana para fusionarlo.
