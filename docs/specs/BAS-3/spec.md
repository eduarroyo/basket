---
codigo: BAS-3
titulo: Despliegue a producción en Azure
estado: Planificado
autor: Eduardo Arroyo
fechaCreacion: 2026-08-04
dependeDe:
  - "[[archive/BAS-2/spec|BAS-2]]"
tags:
  - infraestructura
  - despliegue
---

# BAS-3: Despliegue a producción en Azure

## Descripción

Llevar el esqueleto de BAS-2 a un entorno real de producción en Azure: generar y versionar la infraestructura como código (Bicep vía Aspire/`azd`), aprovisionar Azure Container Apps, Azure SQL Database (Serverless), Azure Key Vault para secretos, configurar GitHub Container Registry como registro de imágenes, y añadir el job de despliegue al workflow de CI/CD existente (`ci.yml`), incluyendo el paso de migración de base de datos y el smoke test E2E previo a la promoción de la imagen. Cloudflare (CDN/WAF de borde) queda fuera de este incremento — se añadirá en `BAS-4` una vez la app esté accesible en Azure y haya un dominio listo.

## Alcance

- Infraestructura como código: `infra/` (Bicep generado por `aspire publish`/`azd`), versionado en el repo (`architecture.md` punto 11).
- Recursos Azure: Container Apps (plan de consumo), Azure SQL Database (tier Serverless, auto-pause), Azure Key Vault, Application Insights/Azure Monitor (`architecture.md` puntos 3, 4, 8).
- GitHub Container Registry (`ghcr.io`) como registro de contenedores, en vez del Azure Container Registry por defecto de `azd` (`architecture.md` punto 12) — incluye gestión de credenciales (login local + secreto en GitHub Actions).
- Managed Identity + Key Vault para secretos (cadena de conexión, credenciales del *seed* del primer administrador) — sustituye a `dotnet user-secrets` como origen de esas credenciales en producción (`architecture.md` punto 7), sin cambiar la lógica del *seed*.
- Job de despliegue en `.github/workflows/ci.yml`: build + push de imagen a GHCR (tag = SHA corto de `main`), `dotnet ef database update` como paso explícito previo al despliegue, smoke E2E (`Tests.E2E`) antes de promocionar, `azd deploy` a Container Apps en modo de revisiones múltiples (`architecture.md` punto 13).
- Un único entorno de producción, mapeado a la rama `main` (`architecture.md` punto 13) — `develop`/`feature/BAS-N` no despliegan a la nube.
- Alertas básicas de Azure Monitor ligadas a los NFR de `functional.md` (latencia p95, tasa de error, fallo de health checks), con notificación por email (`architecture.md` punto 8).

## Fuera de alcance

- Cloudflare (CDN, WAF, rate limiting de borde, Cache Rules) — `BAS-4`, una vez haya un dominio configurado.
- Entorno de staging (descartado explícitamente, `architecture.md` MF-4).
- Monitorización activa de uptime (Availability Tests) — mejora futura opcional, `architecture.md` MF-3.
- Cualquier entidad de negocio nueva o pantalla — sigue sin haberlas hasta `BAS-5` en adelante.

## Criterios de aceptación

- [ ] `infra/` contiene el Bicep generado por Aspire/`azd`, versionado en el repositorio.
- [ ] `azd up`/`azd deploy` aprovisiona y despliega la aplicación en Azure sin pasos manuales no documentados.
- [ ] La aplicación es accesible por HTTPS en la URL pública de Container Apps.
- [ ] Las migraciones de EF Core se aplican como paso explícito del pipeline antes de desplegar, no al arrancar cada instancia.
- [ ] La cadena de conexión y las credenciales del *seed* del primer administrador se leen de Key Vault vía Managed Identity, no de variables de entorno en claro.
- [ ] El *seed* idempotente crea el primer administrador en el entorno de producción si no existe ninguno, igual que en local.
- [ ] El workflow de GitHub Actions añade un job de despliegue a `main`: build + push de imagen a GHCR + migración + smoke E2E + `azd deploy`.
- [ ] El smoke E2E (`Tests.E2E`) se ejecuta y pasa antes de promocionar la imagen a producción.
- [ ] Application Insights recibe logs/trazas/métricas del entorno desplegado.

## Aclaraciones

- **¿Se sustituye SQL Server por Azure SQL Database también en desarrollo?** → No. Se usa el patrón estándar de Aspire para recursos Azure: `AddAzureSqlServer("sql")` con `.RunAsContainer()` en local (`aspire run`/tests) sigue lanzando un contenedor SQL Server igual que hoy con `AddSqlServer`, sin coste ni base de datos Azure adicional; Azure SQL Database Serverless real solo se aprovisiona en el único entorno de producción (`aspire deploy`). No hay una segunda Azure SQL Database de desarrollo compitiendo por presupuesto ni arriesgando datos de producción — ver tarea correspondiente en `tasks.md`.
- **¿Puede haber pérdida de correlación entre las pruebas (SQL Server en contenedor) y el comportamiento en producción (Azure SQL Database)?** → Sí, riesgo real, no solo teórico: (1) Azure SQL Database lanza errores transitorios de *throttling*/failover que el contenedor local nunca genera — EF Core requiere activar explícitamente `EnableRetryOnFailure()` (estrategia específica para Azure SQL, no activada por defecto por `AddSqlServerDbContext`) para que ese camino de código se ejercite igual en ambos entornos; (2) la auto-pausa del tier Serverless retrasa la primera conexión tras inactividad (hasta ~1 min), algo que no ocurre nunca en local; (3) Azure SQL Database no soporta CLR, SQL Server Agent, *linked servers*/`OPENQUERY`, consultas cross-database de 3-4 partes, entre otros — asimetría de motor documentada por Microsoft, poco probable que afecte a este proyecto por su alcance actual pero real. Mitigación: activar `EnableRetryOnFailure()` en el `DbContext` (mismo comportamiento en ambos entornos) y confiar en el smoke E2E contra el entorno real desplegado (ya exigido como criterio de aceptación) como red de seguridad para el resto de asimetrías.
- **¿El acceso a secretos será transparente entre `dotnet user-secrets` (local) y Key Vault (producción), o hace falta una abstracción propia?** → Transparente, sin abstracción que desarrollar: ambos son proveedores de `IConfiguration` en ASP.NET Core, y el código que consume la configuración (`IConfiguration`/`IOptions<T>`, incluido `IdentitySeeder`) es idéntico sea cual sea el origen. La integración de Aspire lo automatiza en el `AppHost`: `builder.AddAzureKeyVault("kv")` + `.WithReference(kv)` en `Web` inyecta el proveedor de Key Vault en el pipeline de configuración del proyecto referenciado, sin código manual (`AddAzureKeyVault(uri, credential)` explícito no hace falta).
- **¿Se puede emular Azure Key Vault en local para acercar el entorno de desarrollo al de producción?** → No, no existe emulador ni modo contenedor para Azure Key Vault (a diferencia de Azure SQL Database, que sí tiene `RunAsContainer()`, o de Azure App Configuration, que tiene emulador oficial en contenedor) — coherente con ser un servicio gestionado respaldado por HSM, no pensado para replicarse localmente. En local se sigue sin Key Vault, con `dotnet user-secrets` tal cual. Asimetría aceptada: a diferencia de Azure SQL, no hay comportamiento de negocio en tiempo de ejecución que dependa de Key Vault (los secretos solo se leen una vez al arrancar), así que no requiere mitigación adicional.
- **¿Un solo workflow de CI/CD, o se separan las responsabilidades?** → Tres workflows, cada uno con su disparador y su alcance de secretos:
  - `ci.yml` (ya existente, sin cambios): `push`/`pull_request` a `develop` — build + tests unitarios/integración. Sin secretos de Azure.
  - `publish.yml` (nuevo): `push` a `main` — build + tests + build de la imagen + push a GHCR (tag = SHA corto, `architecture.md` punto 13). No toca Azure ni la base de datos. Como último paso, dispara `deploy.yml` vía `workflow_dispatch` (API de GitHub), pasándole el `image_tag` que acaba de publicar.
  - `deploy.yml` (nuevo): disparado **únicamente** por `workflow_dispatch`, con un input obligatorio `image_tag` — nunca reacciona directamente a un push. Pasos: `dotnet ef database update` contra producción, smoke E2E contra el entorno recién desplegado, `aspire deploy`/`azd deploy` apuntando al tag indicado.
  
  Motivación: separa build/publicación (sin credenciales de Azure) de despliegue (con OIDC), evita condicionar por rama dentro de un mismo fichero, y dado que `deploy.yml` acepta un `image_tag` explícito, sirve tanto para el flujo automático de hoy (cada `publish.yml` dispara su propio despliegue) como para controlar manualmente cuándo se despliega o revertir a una versión anterior el día que haya usuarios en producción — sin cambiar el mecanismo, solo invocándolo a mano con un tag distinto.
  
  **Advertencia documentada — rollback no es gratis**: `deploy.yml` ejecuta `dotnet ef database update` en cada invocación, que es *forward-only* (aplica migraciones pendientes, nunca las deshace). Revertir a una imagen antigua después de que el esquema haya avanzado con una migración posterior puede dejar código antiguo corriendo contra un esquema con el que no es compatible (columnas que no espera, restricciones nuevas...). Revertir la aplicación sin revertir también la base de datos es un riesgo conocido, sin mitigar en este incremento — a tener en cuenta antes de usar `deploy.yml` para revertir una vez haya datos reales de producción en juego.
- **¿Cómo se promociona código de `develop` a `main`, y hace falta una rama `release` intermedia?** → Sin rama `release`: su propósito habitual (congelar alcance para estabilizar un candidato mientras el resto del equipo sigue en `develop`, dar tiempo a validar contra *staging*) no aplica aquí — un único contribuyente y sin entorno de *staging* (`architecture.md` MF-4). La promoción es un PR de `develop` a `main`, con las mismas reglas de protección que ya rigen para `feature/BAS-N`→`develop`:
  - Ambas ramas, `main` y `develop`, protegidas en GitHub: sin *push* directo, PR obligatorio, al menos una aprobación de un desarrollador, y la pipeline de CI (build + tests) en verde como requisito para poder fusionar. La configuración de las reglas de protección en GitHub la gestiona directamente el propietario del repositorio, fuera del alcance de las tareas de este incremento.
  - `ci.yml` extiende su disparador `pull_request`/`push` para cubrir también `main`, con el filtro de ramas explícito `branches: [develop, main]` — no cualquier PR dispara la pipeline, así que un PR con destino a una rama secundaria (`feature/BAS-N`, `hotfix/*`) no la ejecuta.
  - `claude-code-review.yml` no necesita cambios: ya se ejecuta en cualquier PR sin filtrar por rama destino.
- **¿`infra/` contendrá el Bicep generado por Aspire/`azd`, versionado en el repositorio?** → No, la premisa era incorrecta (corregido también en `architecture.md` punto 11). Por defecto, tanto `aspire deploy` como `azd provision`/`azd deploy` generan el Bicep **en memoria** en el momento del despliegue a partir de `AppHost.cs`, sin escribirlo a disco ([fuente](https://learn.microsoft.com/dotnet/aspire/deployment/azd/aca-deployment-azd-in-depth#how-azure-developer-cli-integration-works)); ningún tutorial oficial de `aspire deploy` menciona una carpeta `infra/`. `AppHost.cs`, ya versionado, es la fuente real de infraestructura como código — es la idea central de Aspire, declarar la topología en C# en vez de escribir Bicep a mano. Materializar el Bicep a disco es un paso aparte y opcional (`azd infra gen`), pensado para cuando hace falta personalizar recursos más allá de la API de Aspire; una vez generado deja de sincronizarse solo con `AppHost.cs` (regenerar sobrescribe cualquier cambio manual). No se usa en este incremento. Se elimina `infra/` del alcance y de los criterios de aceptación.
- **¿BAS-3 incluye Cloudflare?** → No. Se decidió partir en dos incrementos más pequeños y verificables: `BAS-3` deja la app accesible en la URL de Container Apps; Cloudflare se añade en `BAS-4` una vez haya un dominio listo.
- **¿Hace falta una suscripción de Azure activa para completar este incremento?** → Sí. En el momento de la propuesta no había ninguna accesible (`az login` fallaba con `No subscriptions found for eduarroyo@outlook.com`); **resuelto** — `az login` ya funciona y hay una suscripción activa (`basic`, `ce358a47-4bfd-46cb-b4cf-d54e092d08b9`, tenant `992cb75b-2b82-4bfe-8df3-20b01901b214`). `azd` todavía no está instalado en esta máquina, pero el plan empieza por `aspire deploy` nativo (ver `plan.md`), que no lo requiere de entrada.

## Referencias

- [[architecture]] — puntos 3, 4, 6, 7, 8, 11, 12, 13, 17.
- [[archive/BAS-2/spec|BAS-2]] — incremento del que depende (scaffolding de la solución sobre el que se despliega).
