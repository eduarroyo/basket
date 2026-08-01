---
name: aspire
description: Comandos y convenciones de la CLI de .NET Aspire (`aspire`) para BasketBaseTracker — scaffolding de AppHost/ServiceDefaults, desarrollo local, recursos Azure, publicación y despliegue. Usar siempre que haya que crear o modificar proyectos Aspire, o ejecutar/publicar la solución.
---

# Aspire CLI

Usar siempre los comandos de la CLI de Aspire para crear/gestionar proyectos Aspire — nunca escribir a mano el `.csproj`/`Program.cs` de un AppHost o ServiceDefaults.

## Instalación (Windows)

```powershell
irm https://aspire.dev/install.ps1 | iex
```

Verificar instalación: `aspire --version`.

## Scaffolding inicial (BAS-1)

Plantilla recomendada para este proyecto: `aspire-empty` — genera solo AppHost + ServiceDefaults, sin proyecto de ejemplo (el proyecto `Web` se añade aparte, ver skill `dotnet`):

```bash
aspire new aspire-empty --name BasketBaseTracker --output .
```

Alternativa con plantillas individuales (útil si se añaden a una solución ya existente en vez de crear una nueva):

```bash
aspire new aspire-apphost --name BasketBaseTracker.AppHost
aspire new aspire-servicedefaults --name BasketBaseTracker.ServiceDefaults
```

## Añadir el proyecto Web al AppHost

1. Crear `BasketBaseTracker.Web` con `dotnet new webapp` (ver skill `dotnet`).
2. Referenciarlo desde el `AppHost`:
   ```csharp
   builder.AddProject<Projects.BasketBaseTracker_Web>("web");
   ```
3. Desde `Web`, añadir referencia a `ServiceDefaults` y llamar a `builder.AddServiceDefaults()` en su `Program.cs`.

## Recursos Azure (Azure SQL, Key Vault) — BAS-2 y siguientes

Usar `aspire add` para integraciones oficiales en vez de escribir a mano el paquete NuGet y el código de registro:

```bash
aspire add azure-sql
aspire add azure-keyvault
```

Confirmar el paquete exacto y la API generada con `aspire add --help` y revisando el diff antes de continuar — las integraciones evolucionan entre versiones.

## Registro de contenedores externo (GHCR)

Decisión registrada en `docs/architecture.md`: usar `AddContainerRegistry`/`WithContainerRegistry` para apuntar a `ghcr.io` en vez del Azure Container Registry por defecto. Es una API experimental (diagnóstico `ASPIRECOMPUTE003`) — comprobar que sigue existiendo con esa firma en la versión instalada antes de usarla.

## Desarrollo local

```bash
aspire run
```

Levanta el AppHost, construye los recursos y abre el dashboard local (logs/trazas/métricas OpenTelemetry).

## Publicación y despliegue

```bash
aspire publish   # genera manifiesto/artefactos de despliegue (incluye el Bicep para infra/)
aspire deploy    # despliega vía azd
```

## Otras utilidades

- `aspire logs` — trazas en vivo
- `aspire otel` — inspeccionar datos OpenTelemetry capturados en local
- `aspire config` / `aspire secret` — configuración y secretos de usuario en local
- `aspire init` — añade soporte Aspire a un repositorio ya existente (no aplica al scaffolding inicial de BAS-1, que parte de cero)

## Nota de versión

Aspire evoluciona rápido (riesgo ya anotado en `docs/architecture.md`). Antes de ejecutar un comando de esta skill, comprobar `aspire --version` y `aspire <comando> --help` por si la sintaxis ha cambiado desde que se escribió este fichero.
