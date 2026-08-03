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

## Scaffolding inicial (BAS-2)

`aspire new` (el subcomando de la CLI de Aspire) solo expone unas pocas plantillas seleccionadas (`aspire-starter`, `aspire-py-starter`, `aspire-apphost-singlefile` en la versión 13.x) — **no** incluye la plantilla vacía de AppHost+ServiceDefaults de dos proyectos que usa este repositorio. Para esa, usar `dotnet new` directamente (sigue siendo una de las dos CLIs mandatadas por `CLAUDE.md`, y sigue sin escribirse nada a mano):

```bash
dotnet new aspire -n BasketBaseTracker -o src
```

Genera `src/BasketBaseTracker.AppHost/`, `src/BasketBaseTracker.ServiceDefaults/`, un `.sln` y un `aspire.config.json` — sin proyecto de ejemplo. El `.sln` y el `aspire.config.json` se generan dentro de `src/`; hay que moverlos a la raíz del repo para que `aspire run` funcione desde ahí y la solución incluya luego `tests/` como hermano de `src/`:

```bash
mv src/BasketBaseTracker.sln BasketBaseTracker.sln
dotnet sln BasketBaseTracker.sln remove "BasketBaseTracker.AppHost\BasketBaseTracker.AppHost.csproj" "BasketBaseTracker.ServiceDefaults\BasketBaseTracker.ServiceDefaults.csproj"
dotnet sln BasketBaseTracker.sln add src/BasketBaseTracker.AppHost/BasketBaseTracker.AppHost.csproj src/BasketBaseTracker.ServiceDefaults/BasketBaseTracker.ServiceDefaults.csproj
```

Este proyecto usa el formato `.slnx` (no el `.sln` clásico) — una vez arreglados los paths, migrar y borrar el `.sln`:

```bash
dotnet sln BasketBaseTracker.sln migrate
rm BasketBaseTracker.sln
```

Genera `BasketBaseTracker.slnx`. A partir de aquí, todos los comandos `dotnet sln`/`dotnet build`/`dotnet test` de este documento y del skill `dotnet` se ejecutan igual pero apuntando a `BasketBaseTracker.slnx` en vez de al `.sln`.

Y actualizar manualmente la ruta dentro de `aspire.config.json` al moverlo a la raíz (es un fichero de configuración de la CLI, no un artefacto de build — sí se puede editar a mano):

```json
{ "appHost": { "path": "src/BasketBaseTracker.AppHost/BasketBaseTracker.AppHost.csproj" } }
```

**Vulnerabilidad conocida al generar**: la plantilla puede fijar `Aspire.AppHost.Sdk` en una versión cuya dependencia transitiva `MessagePack` tiene advisories de seguridad conocidas (NU1902/NU1903 al hacer `dotnet restore`). Comprobar la versión más reciente publicada (`dotnet package search Aspire.AppHost.Sdk` o la API de NuGet) y subir la primera línea del `.csproj` del AppHost (`<Project Sdk="Aspire.AppHost.Sdk/X.Y.Z">`) a esa versión antes de continuar — es una versión de SDK, no una estructura de proyecto, así que editarla a mano es aceptable. Verificar con `dotnet restore` que las advertencias desaparecen.

Plantillas individuales (útil si se añaden a una solución ya existente en vez de crear una nueva, vía `dotnet new` porque `aspire new` tampoco las expone como subcomandos):

```bash
dotnet new aspire-apphost -n BasketBaseTracker.AppHost -o src/BasketBaseTracker.AppHost
dotnet new aspire-servicedefaults -n BasketBaseTracker.ServiceDefaults -o src/BasketBaseTracker.ServiceDefaults
```

## Añadir el proyecto Web al AppHost

1. Crear `BasketBaseTracker.Web` con `dotnet new webapp` (ver skill `dotnet`).
2. Referenciarlo desde el `AppHost`:
   ```csharp
   builder.AddProject<Projects.BasketBaseTracker_Web>("web");
   ```
3. Desde `Web`, añadir referencia a `ServiceDefaults` y llamar a `builder.AddServiceDefaults()` en su `Program.cs`.

## Recursos Azure (Azure SQL, Key Vault) — BAS-3 y siguientes

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
- `aspire init` — añade soporte Aspire a un repositorio ya existente (no aplica al scaffolding inicial de BAS-2, que parte de cero)

## Nota de versión

Aspire evoluciona rápido (riesgo ya anotado en `docs/architecture.md`). Antes de ejecutar un comando de esta skill, comprobar `aspire --version` y `aspire <comando> --help` por si la sintaxis ha cambiado desde que se escribió este fichero.
