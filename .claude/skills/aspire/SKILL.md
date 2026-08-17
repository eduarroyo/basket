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

**Nota (comprobado en `aspire 13.4.6`):** `aspire new` ya expone una plantilla `aspire-empty` ("Empty AppHost (Choose language...)") que cubre el caso de AppHost+ServiceDefaults vacío de dos proyectos. En BAS-2 esa plantilla no existía todavía (solo `aspire-starter`/`aspire-py-starter`/`aspire-apphost-singlefile`) y por eso se usó el workaround con `dotnet new` descrito abajo. **Para cualquier scaffolding inicial futuro, probar primero `aspire new aspire-empty -n <Nombre> -o src`** y comprobar si genera ya la estructura de dos proyectos con `.slnx` correcto antes de repetir el workaround manual. Lo que sigue queda como referencia histórica de cómo se resolvió en BAS-2.

`aspire new` (el subcomando de la CLI de Aspire) solo exponía unas pocas plantillas seleccionadas (`aspire-starter`, `aspire-py-starter`, `aspire-apphost-singlefile` en la versión 13.x usada en BAS-2) — **no** incluía la plantilla vacía de AppHost+ServiceDefaults de dos proyectos que usa este repositorio. Para esa, se usó `dotnet new` directamente (sigue siendo una de las dos CLIs mandatadas por `CLAUDE.md`, y sigue sin escribirse nada a mano):

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

Confirmar el paquete exacto y la API generada con `aspire add --help` y revisando el diff antes de continuar — las integraciones evolucionan entre versiones. Para descubrir el nombre exacto de una integración (o comprobar que sigue existiendo con ese nombre), usar:

```bash
aspire integration search <término>   # p.ej. "sql", "keyvault"
aspire integration list               # listado completo de integraciones disponibles
```

## Registro de contenedores externo (GHCR)

Decisión registrada en `docs/architecture.md`: usar `AddContainerRegistry`/`WithContainerRegistry` para apuntar a `ghcr.io` en vez del Azure Container Registry por defecto. Es una API experimental (diagnóstico `ASPIRECOMPUTE003`) — comprobar que sigue existiendo con esa firma en la versión instalada antes de usarla.

## Desarrollo local

```bash
aspire run
```

Levanta el AppHost, construye los recursos y abre el dashboard local (logs/trazas/métricas OpenTelemetry). Bloquea la terminal en primer plano; se detiene con `Ctrl+C`.

Para lanzarlo en segundo plano y detenerlo desde otra terminal/sesión (comprobado en `aspire 13.4.6`):

```bash
aspire start            # arranca el AppHost en background
aspire ps                # lista AppHosts en ejecución (ruta, PID, panel)
aspire stop              # detiene el AppHost en ejecución limpiamente
```

`aspire stop` es la forma correcta de parar un AppHost colgado o lanzado en background — **ya no hace falta matar el proceso con `taskkill`/`Stop-Process`**, `aspire stop` cierra el AppHost (y sus recursos) de forma ordenada. Si `aspire ps` no encuentra nada pero sigue habiendo un proceso `dotnet` huérfano, ahí sí recurrir a matar el proceso manualmente como último recurso.

## Consultar documentación de Aspire desde la terminal

`aspire docs` da acceso a la documentación oficial de aspire.dev sin salir del flujo de trabajo, y sin depender de adivinar rutas (`WebFetch` sobre una URL de referencia inventada puede dar 404 — mejor usar esto primero cuando haya dudas sobre un comando, integración o API):

```bash
aspire docs search "<palabras clave>"   # busca páginas de documentación, devuelve título + slug ("Espacio") + sección
aspire docs get <slug>                   # contenido completo en markdown de una página, usando el slug de search
aspire docs list                         # lista TODAS las páginas (~500) — evitar, usar search en su lugar
```

Y para la referencia de API (tipos/miembros de los paquetes de Aspire):

```bash
aspire docs api search "<palabras clave>"
aspire docs api get <id>
aspire docs api list <scope>
```

Preferir `aspire docs search`/`get` a buscar en la web cuando se necesite confirmar sintaxis, opciones de una integración o comportamiento de una API de Aspire — es más fiable que adivinar URLs de aspire.dev.

## Publicación y despliegue

```bash
aspire publish   # genera manifiesto/artefactos de despliegue (incluye el Bicep para infra/) — Preview
aspire deploy    # despliega vía azd — Preview
aspire destroy   # destruye un entorno desplegado previamente
```

## Catálogo de comandos (comprobado en `aspire 13.4.6` vía `aspire --help` y https://aspire.dev/reference/cli/overview/)

Comandos de aplicación:
- `aspire new` — crear un proyecto Aspire desde plantilla (ver scaffolding arriba).
- `aspire init` — añade soporte Aspire a un repo/solución ya existente (no usado en BAS-2, que parte de cero).
- `aspire add [<integración>]` — añade una integración de hosting al AppHost.
- `aspire update` — actualiza las integraciones del proyecto Aspire (Preview).
- `aspire restore` — restaura dependencias y genera el código de SDK de un AppHost.
- `aspire run` — ejecuta el AppHost en primer plano (modo desarrollo).
- `aspire start` / `aspire stop` — arranca/detiene el AppHost en background.
- `aspire ls` — lista ficheros de proyecto AppHost candidatos en el workspace (no confundir con `aspire ps`, que lista los que están *en ejecución*).
- `aspire ps` — lista los AppHosts en ejecución (ruta, PID, panel).

Administración de recursos:
- `aspire resource <recurso> <comando>` — ejecuta un comando sobre un recurso concreto.
- `aspire wait <recurso>` — bloquea hasta que un recurso alcance el estado esperado.

Supervisión:
- `aspire describe [<recurso>]` — instantánea del estado de los recursos de un AppHost en ejecución.
- `aspire logs [<recurso>]` — trazas en vivo.
- `aspire otel` — inspecciona datos de OpenTelemetry (logs/spans/traces) capturados en local.
- `aspire export [<recurso>]` — exporta telemetría y datos de recursos a un zip.

Herramientas y configuración:
- `aspire config` — gestiona opciones de configuración de la CLI (`get`/`set`/`delete`/`list`).
- `aspire secret` — gestiona secretos de usuario del AppHost (`get`/`set`/`delete`/`list`/`path`).
- `aspire certs` — gestiona certificados de desarrollo HTTPS (`trust`/`clean`).
- `aspire cache` — gestiona la caché en disco de la CLI (`clear`).
- `aspire docs` — busca en la documentación de aspire.dev (`search`/`list`/`get`) y en la referencia de API (`api search`/`api get`/`api list`) — ver sección dedicada arriba.
- `aspire doctor` — diagnostica problemas del entorno y la configuración de Aspire.
- `aspire integration` — gestiona/descubre integraciones de hosting (`add`/`list`/`search`, ver sección de recursos Azure arriba).
- `aspire agent` — gestiona integraciones de agentes de IA.
- `aspire mcp` — **en desuso**, usar `aspire agent` en su lugar.
- `aspire dashboard` — gestiona el dashboard de Aspire (Preview).
- `aspire do [<paso>]` — ejecuta un paso concreto de la pipeline y sus dependencias (Preview).

## Nota de versión

Aspire evoluciona rápido (riesgo ya anotado en `docs/architecture.md`). Antes de ejecutar un comando de esta skill, comprobar `aspire --version` y `aspire <comando> --help` por si la sintaxis ha cambiado desde que se escribió este fichero. Última revisión completa de comandos: 2026-08-03, contra `aspire 13.4.6` y https://aspire.dev/reference/cli/overview/.
