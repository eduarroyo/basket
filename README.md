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

### Arranque en local

Requisitos previos: SDK de .NET 10 estable (ver `global.json`), Docker Desktop (o equivalente) para el contenedor de SQL Server, y certificado HTTPS de desarrollo (`dotnet dev-certs https --trust`, una vez por máquina).

1. `dotnet tool restore` — instala en el repo las herramientas de línea de comandos versionadas en `.config/dotnet-tools.json` (`dotnet-ef`, `dotnet-aspnet-codegenerator`, `aspire.cli`).
2. Configurar las credenciales del primer administrador. No hay pantalla de alta pública: la primera cuenta la crea un *seed* idempotente al arrancar, leyendo `user-secrets` en local (`docs/architecture.md`, punto 7):
   ```bash
   dotnet user-secrets set "Seed:AdminEmail" "tu-email@ejemplo.com" --project src/BasketBaseTracker.Web
   dotnet user-secrets set "Seed:AdminPassword" "UnaClaveDeAlMenos12Caracteres!" --project src/BasketBaseTracker.Web
   ```
   Sin esto, el arranque falla con un error explícito indicando qué configurar (no arranca con un administrador sin credenciales conocidas).
3. `aspire run` desde la raíz del repo — levanta `Web` y el contenedor local de SQL Server con dashboard de logs/trazas/métricas.

### Migraciones de base de datos

- **En `Development`**: se aplican automáticamente al arrancar la aplicación (`Program.cs` llama a `Database.MigrateAsync()` antes del *seed* del administrador). No hace falta ningún paso manual, ni siquiera después de borrar el volumen de datos de SQL Server — un `aspire run` sobre una base de datos vacía crea el esquema solo.
- **En cualquier otro entorno** (producción): nunca se aplican al arrancar la aplicación — `docs/architecture.md` (punto 13) lo evita explícitamente, para que varias réplicas de Container Apps no intenten migrar a la vez en un pico de tráfico. Se aplican como paso explícito y controlado del pipeline de despliegue, antes de publicar la nueva revisión:
  ```bash
  dotnet ef database update --project src/BasketBaseTracker.Web --connection "<cadena de conexión del entorno>"
  ```

Para crear una migración nueva (en cualquier entorno, no depende de development/producción):

```bash
dotnet ef migrations add <Nombre> --project src/BasketBaseTracker.Web
```

## Estado del proyecto

En desarrollo activo. Diseño (requisitos, modelo de datos, inventario de pantallas, arquitectura) completo; implementación en curso — ver el incremento en curso en `docs/specs/`.

## Recursos de desarrollo

Proyecto voluntario sin financiación, desarrollado y mantenido por una única persona. Ver [`docs/functional.md`](docs/functional.md#recursos-de-desarrollo) para más detalle.
