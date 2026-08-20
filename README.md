# BasketBaseTracker

BasketBaseTracker es una plataforma de seguimiento de la competición de baloncesto mancomunado de la provincia de Sevilla.

Permite consultar de forma pública calendarios, resultados y clasificaciones de las distintas categorías (benjamín, alevín, infantil, cadete, juvenil...) a lo largo de la temporada, y ofrece un panel de administración para gestionar temporadas, categorías, clubes, equipos, sedes, calendarios y resultados. No se gestionan datos personales: los jugadores se tratan como atributos anónimos de cada equipo (dorsal, posición), no como entidades con identidad propia.

## Entregables

| Entregable | URL |
|---|---|
| Repositorio | <https://github.com/eduarroyo/basket> |
| Producción | _pendiente_ |
| Slides | _pendiente_ |
| Vídeo | _pendiente_ |

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

> [!NOTE]
> El entorno de producción está desplegado con recursos mínimos para mantener el coste en cero. **Azure Container Apps** funciona con *scale-to-zero* (0 réplicas en reposo), y **Azure SQL Database** usa el tier **Serverless** con *auto-pause*. La primera petición tras un periodo de inactividad puede tardar varios segundos en responder mientras se produce el *cold start* del Container App y el *auto-resume* de la base de datos — es el comportamiento esperado, no un fallo del servicio.

## Funcionalidades principales

- **Consulta pública** (sin autenticación): calendarios de competición por jornada, resultados (marcador y parciales por cuarto), clasificación calculada por categoría, ficha de equipo/club/sede, y suscripción a calendario en formato iCal por competición o por equipo.
- **Panel de administración** (autenticado): gestión de temporadas, categorías, clubes, sedes, competiciones, equipos, plantillas (dorsal/posición, sin datos personales), planificación de calendarios y jornadas, e introducción de resultados (incluida la resolución administrativa de partidos aplazados, cancelados o con victoria declarada sin disputarse).
- **Penalizaciones de clasificación**: ajustes de puntos a un equipo con motivo y, opcionalmente, partido vinculado.
- **Importación/exportación completa** de todos los datos del sistema, para backup o migración — solo accesible al rol `Administrador`.
- **Observabilidad** vía OpenTelemetry (logs, métricas, trazas) exportada a Application Insights.

Inventario completo de pantallas en [`docs/screens.md`](docs/screens.md).

## Estructura del proyecto

```
├── src/
│   ├── BasketBaseTracker.AppHost/           # Orquestación .NET Aspire (local y despliegue)
│   ├── BasketBaseTracker.ServiceDefaults/   # Configuración compartida (OpenTelemetry, health checks, resiliencia)
│   ├── BasketBaseTracker.Web/               # Aplicación ASP.NET Core (Razor Pages, Areas Public/Admin, EF Core)
│   └── BasketBaseTracker.Seed/              # Comando de consola para poblar datos de demostración
├── tests/                                   # Proyectos de test
├── docs/
│   ├── specs/                               # Incrementos BAS-N (spec → plan → tasks) y specs/archive/ para los cerrados
│   ├── functional.md, data-model.md, screens.md, architecture.md, workflow.md
├── global.json                              # Versión de SDK de .NET fijada
└── aspire.config.json                       # Configuración de la CLI de Aspire
```

## Reglas de colaboración

El desarrollo sigue un flujo dirigido por especificaciones (spec-driven development), detallado en [`docs/workflow.md`](docs/workflow.md). En resumen:

- El trabajo se organiza en incrementos, cada uno identificado con un código secuencial `BAS-N` que se referencia en commits, ramas y PRs (p. ej. `BAS-2: competiciones y equipos`).
- Cada incremento vive en `docs/specs/BAS-N/` con tres ficheros: `spec.md` (qué y por qué), `plan.md` (cómo) y `tasks.md` (tareas concretas y verificables).
- Ciclo por incremento: **propuesta → aclaración → plan → tareas → implementación → cierre**. El cierre archiva la carpeta en `docs/specs/archive/` y actualiza `data-model.md`/`screens.md`/`architecture.md` si el incremento introdujo cambios de diseño no anticipados.
- Cada incremento se trabaja en su propia rama `feature/BAS-N`, creada desde `develop` al empezar. Se cierra con un PR a `develop` que **solo se fusiona con confirmación humana explícita**.
- Las specs usan propiedades de Obsidian (frontmatter YAML en `camelCase`) para poder visualizar las dependencias entre incrementos (`dependeDe`) como grafo.

## Cómo contribuir (de un incremento a producción)

Pasos completos desde que se propone un incremento hasta que el código llega a producción — detalle del proceso en [`docs/workflow.md`](docs/workflow.md), de la pipeline en [`docs/architecture.md`](docs/architecture.md#13-cicd):

1. **Crear la rama** `feature/BAS-N` desde `develop`.
2. **Especificar el incremento** en `docs/specs/BAS-N/`: `spec.md` (propuesta + ronda de aclaraciones) → `plan.md` (diseño técnico) → `tasks.md` (tareas verificables), cumpliendo la Definición de Listo antes de empezar a implementar (`docs/workflow.md`).
3. **Implementar** tarea a tarea, marcando checkboxes en `tasks.md`. Cada `push`/PR contra `develop` o `main` dispara `ci.yml` (build + tests unitarios y de integración), que bloquea el merge si falla.
4. **Cerrar el incremento**: mover la carpeta a `docs/specs/archive/`, actualizar `data-model.md`/`screens.md`/`architecture.md` si hubo cambios de diseño no anticipados, y comprobar la Definición de Hecho.
5. **Abrir un PR** de `feature/BAS-N` a `develop`. `main` y `develop` son ramas protegidas (Rulesets de GitHub): PR obligatorio y CI en verde, pero **la fusión siempre exige confirmación humana explícita**, nunca la hace el asistente de IA aunque todo esté completo.
6. **Promocionar a producción**: cuando se decide sacar los incrementos ya en `develop`, se abre un PR de `develop` a `main` (misma regla: fusión solo con confirmación humana). El `push` a `main` dispara automáticamente:
   - `publish.yml` — build, tests, y publica la imagen de contenedor (SDK Container Support, sin Dockerfile) en el Azure Container Registry, etiquetada con el SHA corto del commit.
   - `deploy.yml` — aplica las migraciones de base de datos pendientes, despliega la imagen nueva como revisión de Container Apps **sin tráfico**, ejecuta un smoke test E2E (Playwright) contra esa revisión y, solo si pasa, promociona el 100% del tráfico. Si falla, la revisión queda desplegada pero inactiva — no requiere rollback.

### Arranque en local

Requisitos previos: SDK de .NET 10 estable (ver `global.json`), Docker Desktop (o equivalente) para el contenedor de SQL Server, y certificado HTTPS de desarrollo (`dotnet dev-certs https --trust`, una vez por máquina).

1. `dotnet tool restore` — instala en el repo las herramientas de línea de comandos versionadas en `.config/dotnet-tools.json` (`dotnet-ef`, `dotnet-aspnet-codegenerator`, `aspire.cli`).
2. Configurar las credenciales del primer administrador. No hay pantalla de alta pública: la primera cuenta la crea un *seed* idempotente al arrancar, leyendo `user-secrets` en local (`docs/architecture.md`, punto 7):
   ```bash
   dotnet user-secrets set "Seed:AdminEmail" "tu-email@ejemplo.com" --project src/BasketBaseTracker.Web
   dotnet user-secrets set "Seed:AdminPassword" "UnaClaveDeAlMenos12Caracteres!" --project src/BasketBaseTracker.Web
   ```
   Sin esto, el arranque falla con un error explícito indicando qué configurar (no arranca con un administrador sin credenciales conocidas).

   > **No hay usuario/contraseña de prueba fijos.** El único administrador inicial es el que tú mismo defines aquí — úsalos para entrar en `/Admin/Login` una vez arrancada la aplicación.
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

### Datos de demostración

`src/BasketBaseTracker.Seed` es un comando de consola separado (nunca se ejecuta al arrancar `Web`) que puebla la base de datos con un dataset de demostración completo: varias temporadas de histórico, clubes/equipos/jugadores y una temporada en curso a medio disputar (`docs/specs/archive/BAS-17/spec.md`).

1. Con `aspire run`/`aspire start` levantado (el contenedor local de SQL Server escucha siempre en el puerto fijo `1433`, ver skill `dotnet`), averiguar la contraseña de `sa` del contenedor:
   ```bash
   docker inspect <contenedor-sql> --format '{{range .Config.Env}}{{println .}}{{end}}'   # variable MSSQL_SA_PASSWORD
   ```
2. Configurar la cadena de conexión una vez, vía `user-secrets` propios de este proyecto (no comparte los secretos de `Web`):
   ```bash
   dotnet user-secrets set "ConnectionStrings:basketbasetracker" "Server=127.0.0.1,1433;Database=basketbasetracker;User Id=sa;Password=<...>;TrustServerCertificate=True;" --project src/BasketBaseTracker.Seed
   ```
3. Ejecutar el comando (falla si ya hay datos de competición, salvo que se indique `--reset`; `--reset` exige además `--confirmar BORRAR` como salvaguarda explícita):
   ```bash
   dotnet run --project src/BasketBaseTracker.Seed -- --temporadas 3 --clubes 10
   dotnet run --project src/BasketBaseTracker.Seed -- --temporadas 3 --clubes 10 --reset --confirmar BORRAR
   ```

Contra producción, el mismo comando se dispara como workflow manual de GitHub Actions (`workflow_dispatch`, `.github/workflows/seed-demo.yml`) — nunca a mano desde un puesto local, dado que no existe un entorno de `staging` separado (`docs/architecture.md`, punto 13).

### Configuración

Resiliencia de `ApplicationDbContext` ante fallos transitorios de Azure SQL (auto-resume del tier Serverless, *throttling*, failover — `docs/specs/BAS-3/spec.md`). Se leen de `IConfiguration` bajo la sección `Sql:Resilience` (`appsettings.json`/`appsettings.{Environment}.json`, o la variable de entorno equivalente con `__` en vez de `:`, p. ej. `Sql__Resilience__MaxRetryCount`):

| Variable | Descripción | Rango de valores | Valor recomendado |
|---|---|---|---|
| `Sql:Resilience:MaxRetryCount` | Número máximo de reintentos ante un fallo transitorio antes de propagar la excepción. | 0–10 (0 desactiva los reintentos) | 4 |
| `Sql:Resilience:MaxRetryDelaySeconds` | Techo del retraso entre reintentos (crecimiento exponencial con *jitter* hasta este máximo). | 1–30 | 10 |

Ambos parámetros son opcionales — si no se configuran, se usan los valores recomendados (definidos como valor por defecto en `SqlResilienceOptions`). No subir mucho por encima de estos rangos: Azure Container Apps corta cualquier request HTTP a los 240s de *ingress timeout*, y un número de reintentos/retraso demasiado alto puede hacer que la cadena de reintentos de EF Core no llegue a completarse antes de ese corte.

**¿Se pueden cambiar en caliente, sin reiniciar la app?** No, ni en local ni en producción — corregido tras detectar en producción que la premisa original era incorrecta (`docs/specs/BAS-3/spec.md`). `AddSqlServerDbContext` agrupa los `DbContext` en un *pool* por rendimiento, y EF Core prohíbe sobreescribir `OnConfiguring` cuando el *pooling* está activo (lanza `InvalidOperationException` en el primer uso — nunca llegó a funcionar de verdad, ni siquiera en local, hasta que se detectó en el primer despliegue real que ejercitó este código). Los reintentos se configuran una sola vez al arrancar, vía el parámetro `configureDbContextOptions` de `AddSqlServerDbContext` (`Program.cs`) — cualquier cambio, en cualquier entorno, requiere reiniciar el proceso (en producción, una nueva revisión de Container Apps).

## Estado del proyecto

En desarrollo activo. Diseño (requisitos, modelo de datos, inventario de pantallas, arquitectura) completo; implementación en curso — ver el incremento en curso en `docs/specs/`.

## Recursos de desarrollo

Proyecto voluntario sin financiación, desarrollado y mantenido por una única persona. Ver [`docs/functional.md`](docs/functional.md#recursos-de-desarrollo) para más detalle.
