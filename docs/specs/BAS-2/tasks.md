---
codigo: BAS-2
estado: Planificado
tags:
  - tasks
---

# BAS-2: Tareas

- [x] Crear `AppHost` y `ServiceDefaults` con la CLI de Aspire (skill `aspire`).
- [x] Habilitar Central Package Management (`Directory.Packages.props` en la raíz, `dotnet new packagesprops`).
- [x] Migrar la solución a formato `.slnx` (`dotnet sln migrate`) y eliminar el `.sln` clásico.
- [x] Crear `Directory.Build.props` (`TargetFramework`, `ImplicitUsings`, `Nullable` compartidos) y quitar esas propiedades de los `.csproj` individuales.
- [x] Crear `src/BasketBaseTracker.Web/` (Razor Pages) y añadirlo al `AppHost`.
- [ ] Añadir las Areas `Public` y `Admin` en `Web`.
- [ ] Añadir el recurso de base de datos local al `AppHost` (`AddSqlServer().AddDatabase(...)`) y referenciarlo desde `Web`.
- [ ] Configurar EF Core y el `DbContext` de Identity en `Web`.
- [ ] Añadir ASP.NET Core Identity a `Web` (paquetes, servicios, autenticación por cookies, rol `Administrador`).
- [ ] Generar y aplicar la primera migración de EF Core (esquema de Identity).
- [ ] Scaffoldear solo `Login`/`Logout` de la UI de Identity (sin registro ni recuperación de contraseña).
- [ ] Implementar el seed idempotente del primer administrador, leyendo credenciales de `dotnet user-secrets` en local.
- [ ] Crear la Portada mínima (`/`) en el Area `Public`, accesible sin autenticación.
- [ ] Crear `tests/BasketBaseTracker.Tests/` (xUnit v3 + `Aspire.Hosting.Testing`) con un test de integración que levante el `AppHost` y compruebe el health check de `Web`.
- [ ] Crear `tests/BasketBaseTracker.Tests.E2E/` (xUnit v3 + Playwright) con un test que navegue a la Portada sin autenticarse.
- [ ] Verificar `aspire run`: `AppHost`, `Web` y la base de datos local levantan sin errores.
- [ ] Verificar `dotnet test`: todos los tests (`Tests` y `Tests.E2E`) pasan.
- [ ] Verificar manualmente: el seed crea el primer admin, y ese admin puede iniciar sesión y acceder al Area `Admin`.
- [ ] Crear `.github/workflows/ci.yml` (build + test en push/PR contra `develop`).
- [ ] Verificar que el workflow se dispara y pasa en el PR de este incremento.
- [ ] Revisar la Definición de Hecho (`workflow.md`) antes de abrir el PR a `develop`.
