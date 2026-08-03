---
codigo: BAS-2
estado: Planificado
tags:
  - plan
---

# BAS-2: Plan técnico

## Entidades del modelo de datos afectadas

Ninguna de `data-model.md` — este incremento no introduce entidades de negocio. Sí se crea el esquema propio de ASP.NET Core Identity (`AspNetUsers`, `AspNetRoles`, etc.), que no está documentado en `data-model.md` por ser infraestructura de framework, no un concepto del dominio de la competición.

## Pantallas afectadas

- `Login` (`screens.md`, sección Sistema) — única pantalla de este incremento.
- Portada (`/`) — versión mínima sin contenido real, solo para probar que el Area `Public` sirve páginas sin autenticación.

## Decisiones técnicas específicas de este incremento

- **Identity UI**: se scaffoldea solo `Login`/`Logout` de la librería de clases de Identity, no el flujo completo por defecto (registro, confirmación por email, recuperación de contraseña) — coherente con "sin autorregistro" y "sin confirmación por email" ya decididos en `architecture.md` punto 7.
- **Seed del primer administrador**: guardado condicionado a "si no existe ningún usuario con rol `Administrador`, crear uno". En local, el email/contraseña iniciales se leen de `dotnet user-secrets` (`Seed:AdminEmail`, `Seed:AdminPassword`); en producción esto se sustituye por Key Vault en `BAS-3`, sin cambiar la lógica del seed, solo el origen de la configuración.
- **Recurso de base de datos local**: `AddSqlServer().AddDatabase(...)` en `AppHost` (contenedor SQL Server para desarrollo local), equivalente en forma al Azure SQL Serverless de producción (`architecture.md` punto 3).
- **Estructura de tests**: `tests/BasketBaseTracker.Tests/Unit/` y `.../Integration/` tal como fija `architecture.md` punto 15; el test de integración trivial de este incremento levanta el `AppHost` vía `Aspire.Hosting.Testing` y comprueba que `Web` responde en su endpoint de health check (expuesto por `ServiceDefaults` por convención, sin configuración adicional). El test E2E trivial de `Tests.E2E` navega a la Portada y comprueba que carga sin autenticarse.
- **Workflow de CI**: un único fichero `.github/workflows/ci.yml`, disparado en push y pull request contra `develop`, con un job que hace `dotnet build` + `dotnet test`. Sin job de despliegue (eso lo añade `BAS-3` al mismo fichero).
