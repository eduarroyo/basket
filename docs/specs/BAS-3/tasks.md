---
codigo: BAS-3
estado: Planificado
tags:
  - tasks
---

# BAS-3: Tareas

- [x] **Bloqueante**: conseguir acceso a una suscripción de Azure desde esta máquina (`az login` funcionando, `az account show` devuelve una suscripción válida — suscripción `basic`, `ce358a47-4bfd-46cb-b4cf-d54e092d08b9`).
- [x] Instalar la Aspire CLI más reciente si hace falta y comprobar `aspire --version`/`aspire doctor` antes de empezar (skill `aspire`, "Aspire evoluciona rápido").
- [x] Ejecutar `aspire add azure-appcontainers` en el `AppHost` y configurar `builder.AddAzureContainerAppEnvironment(...)`.
- [x] Exponer `Web` públicamente (`WithExternalHttpEndpoints()`) referenciando el entorno de Container Apps.
- [x] Ejecutar `aspire add azure-sql` y sustituir `AddSqlServer("sql")` por el recurso Azure-aware (contenedor en local con `RunAsContainer()`, Azure SQL Database Serverless real al desplegar — `architecture.md` punto 3), manteniendo el interruptor `Sql:Ephemeral` para los tests (`BAS-2`).
- [x] Ejecutar `aspire add azure-keyvault` y referenciar el Key Vault desde `Web`.
- [x] Comprobar si `AddContainerRegistry`/`WithContainerRegistry` (GHCR) sigue funcionando con la versión de Aspire instalada — sigue funcionando técnicamente, pero `aspire deploy --list-steps` reveló que `AddAzureContainerAppEnvironment` aprovisiona su propio ACR de todos modos (se use o no para las imágenes); GHCR no evita ese coste, así que se revierte a aceptar el ACR por defecto del entorno (`architecture.md` punto 12, revisado).
- [x] `aspire deploy --list-steps` (o equivalente) para revisar en seco qué se va a aprovisionar antes de tocar Azure de verdad.
- [x] Primer aprovisionamiento/despliegue real con `aspire deploy` — con dos ajustes descubiertos durante el despliegue, documentados en `spec.md`: (1) Azure SQL pasa de Managed Identity a autenticación SQL (el script de asignación de roles de Aspire falla de forma determinista con Key Vault presente), con un login de aplicación de privilegios mínimos creado por un paso de *pipeline* propio (`provision-sql-app-login`, `Microsoft.Data.SqlClient` directo, no el módulo de PowerShell roto); (2) nombre de servidor SQL fijo (`basketbasetracker-sql`) para evitar referenciar el output del recurso en tiempo de ejecución. App accesible en `https://web.ashymoss-b1c8f995.spaincentral.azurecontainerapps.io`.
- [x] Configurar en Key Vault las credenciales del *seed* del primer administrador (`Seed:AdminEmail`, `Seed:AdminPassword`) de producción — `Web` las lee vía `AddAzureKeyVaultSecrets("kv")` en `Program.cs` (paquete `Aspire.Azure.Security.KeyVault`).
- [x] Verificar manualmente: la aplicación responde por HTTPS en la URL pública de Container Apps (`curl -I` → `200 OK`).
- [x] Verificar manualmente: `dotnet ef database update` aplica la migración de Identity contra la base de datos de producción como paso explícito (sin arrancar la app) — aplicado a mano con el login admin de SQL como paso puente (la automatización en CI/CD es la tarea de más abajo, aún pendiente).
- [x] Verificar manualmente: el *seed* idempotente crea el primer administrador en producción a partir de las credenciales de Key Vault, y ese administrador puede iniciar sesión — confirmado en la base de datos (`AspNetUsers`/`AspNetRoles`) y con un login real contra `/Admin/Login` (302 a `/` + cookie de autenticación).
- [ ] Revisar el Bicep generado y versionarlo en `infra/`.
- [ ] Configurar autenticación OIDC (Federated Identity) entre GitHub Actions y Azure para el job de despliegue (sin secretos de cliente en claro).
- [ ] Añadir a `.github/workflows/ci.yml` el job de despliegue (disparado solo en push a `main`): build + push de imagen al ACR del entorno (tag = SHA corto) → `dotnet ef database update` → smoke E2E (`Tests.E2E`) → despliegue. Se mantienen los tres workflows (`ci.yml`/`publish.yml`/`deploy.yml`, aclaración de `spec.md` ya resuelta). Dos puntos abiertos a resolver como parte de esta tarea, documentados en `spec.md`: (1) `provision-sql-app-login` necesita una regla de firewall en el servidor SQL para la IP de quien ejecuta el paso — los runners de GitHub Actions no tienen IP fija, hay que abrir el firewall temporalmente durante el job o usar un runner autoalojado con IP conocida; (2) `deploy.yml` no debe usarse para revertir a una imagen antigua sin revertir también el esquema de base de datos — `dotnet ef database update` es *forward-only*, riesgo sin mitigar en este incremento.
- [ ] Configurar alertas básicas de Azure Monitor (latencia p95 > 200ms, tasa de error > 0,5%, fallo de health checks) con notificación por email.
- [ ] Verificar manualmente: Application Insights recibe logs/trazas/métricas del entorno desplegado.
- [ ] Verificar el flujo completo de extremo a extremo: merge a `main` → CI dispara el job de despliegue → build + push + migración + smoke E2E + despliegue → app accesible con los cambios.
- [ ] Revisar la Definición de Hecho (`workflow.md`) antes de abrir el PR a `develop`.
