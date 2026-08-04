---
codigo: BAS-2
titulo: Scaffolding de la solución
estado: Planificado
autor: Eduardo Arroyo
fechaCreacion: 2026-08-03
tags:
  - backend
  - infraestructura
---

# BAS-2: Scaffolding de la solución

## Descripción

Montar el esqueleto de la solución sobre el que se construirán todos los incrementos siguientes: orquestación Aspire para desarrollo local, el proyecto web con sus Areas `Public`/`Admin`, autenticación de administradores, y la base de testing (unitarios, integración y E2E) y de CI descritas en `architecture.md`. Sin entidades de negocio del modelo de datos todavía — es infraestructura, no funcionalidad de la competición.

## Alcance

- `src/BasketBaseTracker.AppHost/` y `src/BasketBaseTracker.ServiceDefaults/` (Aspire), creados con la CLI de Aspire.
- `src/BasketBaseTracker.Web/` (Razor Pages), con las Areas `Public` y `Admin` (arquitectura punto 2).
- Recurso de base de datos local vía Aspire (Azure SQL emulado en contenedor) + EF Core conectado.
- ASP.NET Core Identity: autenticación por cookies, rol único `Administrador` (punto 7), sin autorregistro, con *seed* idempotente del primer administrador a partir de un secreto local (`dotnet user-secrets` en desarrollo — el equivalente a Key Vault en producción es alcance de BAS-3).
- Pantalla `Login` (`screens.md`, sección Sistema).
- Primera migración de EF Core (esquema de ASP.NET Core Identity).
- `tests/BasketBaseTracker.Tests/` (xUnit v3, unitarios e integración con `Aspire.Hosting.Testing`) y `tests/BasketBaseTracker.Tests.E2E/` (Playwright), con al menos un test trivial en cada uno que pruebe que el arnés funciona (arquitectura punto 15).
- Workflow de GitHub Actions: build + test en cada push/PR a `develop`, bloqueando el merge si falla (sin job de despliegue todavía).
- Una página pública mínima (Portada, `/`) accesible sin autenticación, para comprobar que el Area `Public` funciona sin caché ni contenido real todavía.

## Fuera de alcance

- Despliegue a producción e infraestructura Azure real (Bicep, `azd`, Container Apps, Key Vault, Cloudflare) — `BAS-3`.
- Cualquier entidad de negocio del modelo de datos (`Categoria`, `Club`, `Sede`, `Temporada`, `Competicion`, `Equipo`, `FichaJugador`, `Jornada`, `Partido`, `PartidoParcial`, `PenalizacionClasificacion`) y sus pantallas — `BAS-4` en adelante.
- Roles administrativos granulares (MF-7) y 2FA (MF-5) — mejoras futuras, no v1.
- Output Caching de páginas públicas (punto 5) — no hay contenido real todavía que cachear.

## Criterios de aceptación

- [x] `aspire run` levanta `AppHost`, `Web` y el recurso de base de datos local sin errores.
- [x] Existe una migración de EF Core aplicada que crea el esquema de ASP.NET Core Identity.
- [x] Si no existe ningún administrador, el *seed* idempotente crea uno a partir de un secreto local; ese administrador puede iniciar sesión en la pantalla `Login` y acceder al Area `Admin`.
- [x] Un usuario anónimo puede acceder a la Portada (`/`) sin autenticarse.
- [x] `dotnet test` ejecuta los proyectos `Tests` y `Tests.E2E` y todos los tests pasan.
- [ ] Un workflow de GitHub Actions ejecuta build + test en cada push/PR contra `develop` y bloquea el merge si falla. — el workflow existe y pasa (PR #3), pero `develop` no tiene ninguna regla de protección de rama todavía: GitHub permitiría fusionar aunque el check estuviera en rojo. Pendiente de decisión (ver `## Aclaraciones`).

## Aclaraciones

- **¿CI en este incremento o en BAS-3?** → Sí, en `BAS-2`. El primer PR real del proyecto debe llevar CI corriendo desde el principio, en vez de añadirse como parche tardío; `BAS-3` solo añadirá el job de despliegue al workflow que ya exista.
- **¿Identity en este incremento o en BAS-4?** → Sí, en `BAS-2`. Es infraestructura transversal de la que depende cualquier pantalla admin futura (arquitectura puntos 2 y 7) — mejor resolverla una vez aquí que repetirla o improvisarla en cada incremento posterior.

## Referencias

- [[architecture]] — puntos 1, 2, 7, 11, 13, 15.
- [[screens]] — pantalla `Login`.
- [[workflow]] — ciclo del incremento, DoR/DoD.
