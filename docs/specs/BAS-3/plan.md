---
codigo: BAS-3
estado: Planificado
tags:
  - plan
---

# BAS-3: Plan técnico

## Entidades del modelo de datos afectadas

Ninguna de `data-model.md` — como en `BAS-2`, este incremento es infraestructura pura (despliegue), no funcionalidad de la competición.

## Pantallas afectadas

Ninguna nueva. La Portada y `Login`/`Logout` de `BAS-2` pasan a ser accesibles también en producción, sin cambios de comportamiento.

## Decisiones técnicas específicas de este incremento

- **Herramienta de despliegue — validar `aspire deploy` frente a `azd` en el momento de implementar**: `architecture.md` (punto 11) asume `azd` como herramienta de despliegue, pero la documentación actual de Aspire (`aspire docs get deploy-to-azure-container-apps`, comprobado en `aspire 13.4.6`) recomienda `aspire deploy` como camino principal (Aspire aprovisiona directamente el entorno de Container Apps, el registro de contenedores y la identidad administrada), dejando el flujo con `azd` como alternativa para quien ya lo tuviera en marcha ("Use existing azd workflows"). Empezar por `aspire add azure-appcontainers` + `aspire deploy` y comprobar si cubre todo lo que `architecture.md` pide (GHCR en vez de ACR, Key Vault, migraciones como paso explícito); si `aspire deploy` no permite algo de eso, caer al flujo `azd` documentado en `architecture.md`. Si se confirma `aspire deploy` como el camino real, actualizar `architecture.md` punto 11 en el Cierre de este incremento (es exactamente el tipo de deriva que ese punto ya anticipa como riesgo).
- **GHCR en vez de ACR**: `aspire add azure-appcontainers` aprovisiona un Azure Container Registry por defecto — hay que comprobar si sigue siendo posible sustituirlo por GHCR vía `AddContainerRegistry`/`WithContainerRegistry` (API experimental, diagnóstico `ASPIRECOMPUTE003`, `architecture.md` punto 12) con la versión de Aspire instalada en el momento de implementar. Si la API experimental ha cambiado o desaparecido, evaluar si merece la pena seguir forzando GHCR (ahorro de ~5$/mes) o aceptar ACR como coste asumido — decisión a tomar en el momento, no de antemano.
- **Key Vault + Managed Identity para secretos**: `aspire add azure-keyvault` en el `AppHost`, referenciado desde `Web`. La cadena de conexión de Azure SQL ya la inyecta Aspire vía Managed Identity (patrón estándar de `Aspire.Microsoft.EntityFrameworkCore.SqlServer` en Azure); las credenciales del *seed* del primer administrador (`Seed:AdminEmail`, `Seed:AdminPassword`) pasan de `dotnet user-secrets` (local) a secretos de Key Vault expuestos como configuración — mismo código de `IdentitySeeder`, solo cambia el proveedor de configuración según el entorno (ya es así de forma implícita: `IConfiguration` no distingue el origen).
- **Migraciones como paso de pipeline, no al arrancar**: `Program.cs` ya solo aplica migraciones automáticamente en `Development` (`BAS-2`); en producción, el job de despliegue ejecuta `dotnet ef database update` de forma explícita antes de desplegar la nueva revisión, con la cadena de conexión de producción pasada como parámetro (no inyectada por Aspire, ya que el comando corre fuera del `AppHost`) — mismo patrón ya documentado en la skill `dotnet` para aplicar una migración a mano.
- **Workflow de despliegue (`ci.yml`)**: se añade un job nuevo a continuación del job `build-and-test` ya existente, condicionado a push en `main` (no en `develop`/`feature/BAS-N`, `architecture.md` punto 13): build + push de imagen a GHCR (tag = SHA corto), `dotnet test tests/BasketBaseTracker.Tests.E2E` (smoke E2E, único punto donde se ejecuta en CI, `architecture.md` punto 15) contra el entorno recién desplegado o contra un entorno de verificación previo a promocionar — a concretar en `tasks.md` según lo que permita `aspire deploy`/`azd` (¿hay una forma de desplegar sin enrutar tráfico y healthchecar antes de promocionar, o el smoke E2E corre contra producción ya servida por la nueva revisión?). Requiere secretos nuevos en GitHub (credenciales de Azure para el login del job — Federated Identity/OIDC en vez de un secreto de cliente en claro, evita rotar credenciales).
- **Autenticación de GitHub Actions contra Azure**: OIDC (`azure/login` con Federated Credentials), no un secreto de Service Principal en claro — evita gestionar y rotar un secreto adicional, coherente con "Managed Identity en vez de credenciales en claro" ya decidido para la propia app (`architecture.md` punto 6).

## Riesgos y dependencias externas

- **Bloqueante de partida**: no hay ninguna suscripción de Azure accesible desde esta máquina a fecha de la propuesta (`az login` → `No subscriptions found`). Nada de este plan se puede ejecutar de verdad hasta resolverlo — ver `tasks.md`.
- Aspire es un framework joven (`architecture.md` punto 11): varias decisiones de este plan (`aspire deploy` vs `azd`, soporte de GHCR) dependen de comprobar el comportamiento exacto de la versión instalada en el momento de implementar, no se dan por sentadas de antemano.
