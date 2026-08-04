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

- **¿BAS-3 incluye Cloudflare?** → No. Se decidió partir en dos incrementos más pequeños y verificables: `BAS-3` deja la app accesible en la URL de Container Apps; Cloudflare se añade en `BAS-4` una vez haya un dominio listo.
- **¿Hace falta una suscripción de Azure activa para completar este incremento?** → Sí. A fecha de esta propuesta no hay ninguna accesible (`az login` falla con `No subscriptions found for eduarroyo@outlook.com`, y los tenants alternativos exigen MFA no completado). Queda como tarea bloqueante explícita en `tasks.md`; la redacción de `spec.md`/`plan.md`/`tasks.md` no depende de tenerla resuelta todavía.

## Referencias

- [[architecture]] — puntos 3, 4, 6, 7, 8, 11, 12, 13, 17.
- [[archive/BAS-2/spec|BAS-2]] — incremento del que depende (scaffolding de la solución sobre el que se despliega).
