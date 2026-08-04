---
name: dotnet
description: Convenciones de la CLI de .NET para BasketBaseTracker — estructura de solución, build/test, EF Core (migraciones), scaffolding de Razor Pages. Usar siempre que haya que crear proyectos .NET, ejecutar tests, gestionar migraciones de base de datos o generar CRUD.
---

# .NET CLI

## Estructura de la solución

Ver `docs/architecture.md`:

```
src/BasketBaseTracker.AppHost/
src/BasketBaseTracker.ServiceDefaults/
src/BasketBaseTracker.Web/
tests/BasketBaseTracker.Tests/          # Unitarios (Unit/) e integración (Integration/)
tests/BasketBaseTracker.Tests.E2E/      # Smoke E2E con Playwright
```

`AppHost` y `ServiceDefaults` se crean con la plantilla `dotnet new aspire` (ver skill `aspire` — `aspire new` no expone esa plantilla como subcomando en la versión instalada de la CLI, hay que usar `dotnet new` directamente).

## Build, tests y limpieza

```bash
dotnet build
dotnet test
dotnet clean          # limpia bin/ y obj/ del proyecto o solución actual — nunca borrarlas a mano con rm -rf
dotnet format         # aplica el estilo de código por defecto (.editorconfig si existe)
```

## Desarrollo local

```bash
dotnet watch --project src/BasketBaseTracker.Web     # recarga en caliente al editar Web
dotnet dev-certs https --trust                        # certificado HTTPS de desarrollo, una vez por máquina
```

Secretos de usuario en local (p. ej. credenciales del *seed* del primer administrador, `architecture.md` punto 7 — nunca en `appsettings.json`):

```bash
dotnet user-secrets init --project src/BasketBaseTracker.Web
dotnet user-secrets set "Seed:AdminEmail" "admin@example.com" --project src/BasketBaseTracker.Web
dotnet user-secrets list --project src/BasketBaseTracker.Web
```

## Gestión centralizada de paquetes (Central Package Management)

Habilitado desde `BAS-2`: las versiones de los paquetes NuGet viven en `Directory.Packages.props` (raíz del repo), no en cada `.csproj`. Se generó con la propia CLI:

```bash
dotnet new packagesprops
```

Al añadir un paquete nuevo a cualquier proyecto, usar `dotnet add package` como siempre — el SDK de .NET detecta `ManagePackageVersionsCentrally=true` y escribe la versión en `Directory.Packages.props` automáticamente, dejando en el `.csproj` solo `<PackageReference Include="..." />` sin versión:

```bash
dotnet add src/BasketBaseTracker.Web package Microsoft.EntityFrameworkCore.SqlServer
```

Nunca añadir un paquete escribiendo `<PackageReference Include="..." Version="..." />` a mano en un `.csproj` — rompe la gestión centralizada. Referencia: [Central Package Management (Microsoft Learn)](https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management).

Para investigar por qué una versión concreta de un paquete transitivo entra en el árbol de dependencias (p. ej. ante un aviso de seguridad NU1902/NU1903 como el de `MessagePack` al crear el AppHost), usar `dotnet nuget why` en vez de deducirlo a mano:

```bash
dotnet nuget why src/BasketBaseTracker.AppHost/BasketBaseTracker.AppHost.csproj MessagePack
```

Si esa investigación revela que la versión mínima resuelta de una dependencia transitiva tiene una vulnerabilidad conocida (warning `NU1901`-`NU1904` al compilar, con enlace a un GHSA), fijar la versión mínima no vulnerable explícitamente en `Directory.Packages.props`, en vez de ignorar el warning o esperar a que el paquete raíz suba su propia versión mínima:

```xml
<PropertyGroup>
  <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
</PropertyGroup>
<ItemGroup>
  <!-- Transitive dependencies versions forced to avoid vulnerabilities. -->
  <PackageVersion Include="NuGet.Packaging" Version="6.12.5" />
  <PackageVersion Include="NuGet.ProjectModel" Version="6.12.5" />
  <!-- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -->
</ItemGroup>
```

`CentralPackageTransitivePinningEnabled` hace que cualquier `PackageVersion` de `Directory.Packages.props` que coincida con una dependencia *transitiva* (no solo las referenciadas directamente en un `.csproj`) sobrescriba la versión mínima que traería el paquete raíz — así se puede subir solo la dependencia vulnerable a la primera versión que ya no lo es, sin depender de que el paquete que la arrastra (p. ej. `Microsoft.VisualStudio.Web.CodeGeneration.Design`) publique una nueva versión. Referencia: [Central Package Management — Transitive pinning (Microsoft Learn)](https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management#transitive-pinning).

## Propiedades MSBuild compartidas (Directory.Build.props)

`Directory.Build.props` (raíz del repo, generado con `dotnet new buildprops`) centraliza `TargetFramework`, `ImplicitUsings` y `Nullable` para todos los proyectos — no repetirlas en un `.csproj` nuevo, solo las propiedades específicas de ese proyecto (`OutputType`, `UserSecretsId`, etc.). Si un proyecto nuevo necesitara un valor distinto para alguna de estas tres, se sobrescribe en su propio `.csproj` (el `PropertyGroup` del proyecto gana sobre `Directory.Build.props`).

No se usa `Directory.Build.targets` ni `Directory.Solution.props`/`.targets` — no hay todavía un caso de uso real que los justifique (ver `docs/architecture.md`, estructura de la solución).

## Crear el proyecto Web (Razor Pages)

```bash
dotnet new webapp -n BasketBaseTracker.Web -o src/BasketBaseTracker.Web
```

Añadir las Areas `Public` y `Admin` (carpetas `Areas/Public`, `Areas/Admin`) según la organización decidida en `docs/architecture.md`.

## Proyecto de tests

Estrategia completa (qué se testea en cada nivel, cuándo se ejecutan) en `docs/architecture.md#15-estrategia-de-pruebas`.

Unitarios e integración, en `xUnit v3` (plantilla `xunit3`, distinta de la plantilla `xunit` que genera xUnit.net v2):

```bash
dotnet new install xunit.v3.templates
dotnet new xunit3 -n BasketBaseTracker.Tests -o tests/BasketBaseTracker.Tests
dotnet add tests/BasketBaseTracker.Tests reference src/BasketBaseTracker.Web
dotnet add tests/BasketBaseTracker.Tests package Aspire.Hosting.Testing
dotnet add tests/BasketBaseTracker.Tests reference src/BasketBaseTracker.AppHost
```

Smoke E2E, proyecto separado (solo se ejecuta antes de desplegar a producción):

```bash
dotnet new xunit3 -n BasketBaseTracker.Tests.E2E -o tests/BasketBaseTracker.Tests.E2E
dotnet add tests/BasketBaseTracker.Tests.E2E package Microsoft.Playwright
```

## Herramientas .NET (manifiesto local, no `--global`)

Las herramientas CLI del proyecto (`dotnet-ef`, `dotnet-aspnet-codegenerator`) se instalan en un manifiesto local versionado en el repo, no con `--global` — así el workflow de CI (`architecture.md` punto 13) y cualquier máquina nueva instalan exactamente las mismas versiones con `dotnet tool restore`, sin depender de qué tenga instalado global el entorno:

```bash
dotnet new tool-manifest                              # una sola vez, crea .config/dotnet-tools.json
dotnet tool install dotnet-ef
dotnet tool install dotnet-aspnet-codegenerator
dotnet tool restore                                   # en CI o en una máquina nueva, tras clonar el repo
```

Con manifiesto local, cada comando de la herramienta se invoca con `dotnet <herramienta>` igual que si fuera global (`dotnet tool restore` deja los shims listos).

## EF Core

Requiere el paquete `Microsoft.EntityFrameworkCore.Design` en el proyecto (`Web`), con `PrivateAssets="all"` (solo se usa en tiempo de diseño, no debe llegar al artefacto publicado).

Migraciones:

```bash
dotnet ef migrations add <Nombre> --project src/BasketBaseTracker.Web
```

`dotnet ef migrations add` no necesita conexión real (solo el modelo). `dotnet ef database update` sí — y como `Program.cs` usa `AddSqlServerDbContext` (Aspire), la cadena de conexión solo se inyecta automáticamente cuando el proceso lo arranca el `AppHost`. Para aplicar una migración a mano fuera de `aspire run`, hay que pasarla explícitamente:

```bash
dotnet ef database update --project src/BasketBaseTracker.Web --connection "Server=127.0.0.1,<puerto>;Database=basketbasetracker;User Id=sa;Password=<...>;TrustServerCertificate=True;"
```

- Puerto y contraseña del contenedor local: `docker ps` (columna *Ports*) y `docker inspect <contenedor> --format '{{range .Config.Env}}{{println .}}{{end}}'` (variable `MSSQL_SA_PASSWORD`).
- **Usar `127.0.0.1`, nunca `localhost`**, en la cadena de conexión: en Windows, `localhost` puede resolver primero a IPv6 (`::1`), y el puerto publicado por Docker Desktop normalmente solo escucha en IPv4 (`127.0.0.1:<puerto>->1433/tcp`) — con `localhost` la conexión falla por timeout (Error 258) aunque el contenedor esté sano; con `127.0.0.1` funciona a la primera.

## Scaffolding de CRUD (Razor Pages + EF Core)

```bash
dotnet aspnet-codegenerator razorpage -m <Entidad> -dc <DbContext> -udl -outDir Areas/Admin/Pages/<Entidad> --referenceScriptLibraries
```

El scaffolding es un punto de partida, no el resultado final — revisar siempre el código generado antes de darlo por bueno (nombres, validaciones, autorización de la página).

## Nota de versión

Comprobar `dotnet --version` (debe ser .NET 10) antes de crear proyectos nuevos.
