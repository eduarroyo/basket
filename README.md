# BasketBaseTracker

BasketBaseTracker es una plataforma de seguimiento de la competición de baloncesto mancomunado de la provincia de Sevilla.

Permite consultar de forma pública calendarios, resultados y clasificaciones de las distintas categorías (benjamín, alevín, infantil, cadete, juvenil...) a lo largo de la temporada, y ofrece un panel de administración para gestionar temporadas, categorías, clubes, equipos, sedes, calendarios y resultados. No se gestionan datos personales: los jugadores se tratan como atributos anónimos de cada equipo (dorsal, posición), no como entidades con identidad propia.

## Documentación

- [`docs/functional.md`](docs/functional.md) — requisitos funcionales y no funcionales, alcance, usuarios del sistema.
- [`docs/data-model.md`](docs/data-model.md) — modelo de datos (entidades, relaciones, decisiones de diseño).
- [`docs/screens.md`](docs/screens.md) — inventario de pantallas públicas y de administración.
- [`docs/architecture.md`](docs/architecture.md) — decisiones de arquitectura (stack, hosting, seguridad, observabilidad, IaC).
- [`docs/workflow.md`](docs/workflow.md) — flujo de trabajo de desarrollo dirigido por especificaciones (SDD).

## Stack tecnológico

- **Backend/frontend**: .NET 10, ASP.NET Core con Razor Pages (Areas `Public`/`Admin`), EF Core.
- **Base de datos**: Azure SQL Database (tier Serverless).
- **Hosting**: Azure Container Apps (escala a cero).
- **Infraestructura como código**: .NET Aspire + `azd`.
- **Observabilidad**: OpenTelemetry → Application Insights.
- **CDN/seguridad perimetral**: Cloudflare.

Detalles y justificación de cada decisión en [`docs/architecture.md`](docs/architecture.md).

## Reglas de colaboración

El desarrollo sigue un flujo dirigido por especificaciones (spec-driven development), detallado en [`docs/workflow.md`](docs/workflow.md). En resumen:

- El trabajo se organiza en incrementos, cada uno identificado con un código secuencial `BAS-N` que se referencia en commits, ramas y PRs (p. ej. `BAS-2: competiciones y equipos`).
- Cada incremento vive en `docs/specs/BAS-N/` con tres ficheros: `spec.md` (qué y por qué), `plan.md` (cómo) y `tasks.md` (tareas concretas y verificables).
- Ciclo por incremento: **propuesta → aclaración → plan → tareas → implementación → cierre**. El cierre archiva la carpeta en `docs/specs/archive/` y actualiza `data-model.md`/`screens.md`/`architecture.md` si el incremento introdujo cambios de diseño no anticipados.
- Cada incremento se trabaja en su propia rama `feature/BAS-N`, creada desde `develop` al empezar. Se cierra con un PR a `develop` que **solo se fusiona con confirmación humana explícita**.
- Las specs usan propiedades de Obsidian (frontmatter YAML en `camelCase`) para poder visualizar las dependencias entre incrementos (`dependeDe`) como grafo.

## Estado del proyecto

En fase de diseño: requisitos, modelo de datos, inventario de pantallas y arquitectura ya definidos. Aún no hay código — el siguiente paso es montar el esqueleto de la solución.

## Recursos de desarrollo

Proyecto voluntario sin financiación, desarrollado y mantenido por una única persona. Ver [`docs/functional.md`](docs/functional.md#recursos-de-desarrollo) para más detalle.
