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
tests/BasketBaseTracker.Tests/
```

`AppHost` y `ServiceDefaults` se crean con la CLI de Aspire (ver skill `aspire`), no con `dotnet new` directamente.

## Build y tests

```bash
dotnet build
dotnet test
```

## Crear el proyecto Web (Razor Pages)

```bash
dotnet new webapp -n BasketBaseTracker.Web -o src/BasketBaseTracker.Web
```

Añadir las Areas `Public` y `Admin` (carpetas `Areas/Public`, `Areas/Admin`) según la organización decidida en `docs/architecture.md`.

## Proyecto de tests

```bash
dotnet new xunit -n BasketBaseTracker.Tests -o tests/BasketBaseTracker.Tests
dotnet add tests/BasketBaseTracker.Tests reference src/BasketBaseTracker.Web
```

## EF Core

Instalar la herramienta si no está disponible:

```bash
dotnet tool install --global dotnet-ef
```

Migraciones:

```bash
dotnet ef migrations add <Nombre> --project src/BasketBaseTracker.Web
dotnet ef database update --project src/BasketBaseTracker.Web
```

## Scaffolding de CRUD (Razor Pages + EF Core)

```bash
dotnet tool install -g dotnet-aspnet-codegenerator
dotnet aspnet-codegenerator razorpage -m <Entidad> -dc <DbContext> -udl -outDir Areas/Admin/Pages/<Entidad> --referenceScriptLibraries
```

El scaffolding es un punto de partida, no el resultado final — revisar siempre el código generado antes de darlo por bueno (nombres, validaciones, autorización de la página).

## Nota de versión

Comprobar `dotnet --version` (debe ser .NET 10) antes de crear proyectos nuevos.
