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
- [ ] Ejecutar `aspire add azure-keyvault` y referenciar el Key Vault desde `Web`.
- [ ] Comprobar si `AddContainerRegistry`/`WithContainerRegistry` (GHCR) sigue funcionando con la versión de Aspire instalada; si no, documentar la decisión de aceptar ACR o buscar alternativa.
- [ ] `aspire deploy --list-steps` (o equivalente) para revisar en seco qué se va a aprovisionar antes de tocar Azure de verdad.
- [ ] Primer aprovisionamiento/despliegue real con `aspire deploy` (o `azd` si `aspire deploy` no cubre algo del plan — ver `plan.md`).
- [ ] Configurar en Key Vault las credenciales del *seed* del primer administrador (`Seed:AdminEmail`, `Seed:AdminPassword`) de producción.
- [ ] Verificar manualmente: la aplicación responde por HTTPS en la URL pública de Container Apps.
- [ ] Verificar manualmente: `dotnet ef database update` aplica la migración de Identity contra la base de datos de producción como paso explícito (sin arrancar la app).
- [ ] Verificar manualmente: el *seed* idempotente crea el primer administrador en producción a partir de las credenciales de Key Vault, y ese administrador puede iniciar sesión.
- [ ] Revisar el Bicep generado y versionarlo en `infra/`.
- [ ] Configurar autenticación OIDC (Federated Identity) entre GitHub Actions y Azure para el job de despliegue (sin secretos de cliente en claro).
- [ ] Añadir a `.github/workflows/ci.yml` el job de despliegue (disparado solo en push a `main`): build + push de imagen a GHCR (tag = SHA corto) → `dotnet ef database update` → smoke E2E (`Tests.E2E`) → despliegue.
- [ ] Configurar alertas básicas de Azure Monitor (latencia p95 > 200ms, tasa de error > 0,5%, fallo de health checks) con notificación por email.
- [ ] Verificar manualmente: Application Insights recibe logs/trazas/métricas del entorno desplegado.
- [ ] Verificar el flujo completo de extremo a extremo: merge a `main` → CI dispara el job de despliegue → build + push + migración + smoke E2E + despliegue → app accesible con los cambios.
- [ ] Revisar la Definición de Hecho (`workflow.md`) antes de abrir el PR a `develop`.
