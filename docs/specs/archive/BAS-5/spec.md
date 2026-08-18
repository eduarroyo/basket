---
codigo: BAS-5
titulo: Modelo de datos de dominio
estado: Archivado
autor: Eduardo Arroyo
fechaCreacion: 2026-08-18
dependeDe:
  - "[[archive/BAS-3/spec|BAS-3]]"
tags:
  - backend
  - datos
---

# BAS-5: Modelo de datos de dominio

## Descripción

BAS-1 dejó definido el modelo de datos completo en `data-model.md`, y BAS-3 dejó desplegada la aplicación con `ApplicationDbContext` funcionando pero limitado a `IdentityDbContext` (autenticación de administradores) — sin ninguna entidad de dominio todavía. Este incremento añade las once entidades de `data-model.md` (catálogo, ancladas a temporada, calendario/resultados y clasificación) al `ApplicationDbContext` existente, con sus restricciones (claves únicas, concurrencia optimista) y la migración de EF Core correspondiente. Es un incremento puramente de esquema: sienta la base sobre la que colgarán las pantallas de Admin y públicas de incrementos futuros, sin construir ninguna pantalla ni lógica de negocio todavía.

## Alcance

- Clases de entidad para las once tablas de `data-model.md`: `Categoria`, `Club`, `Sede`, `Temporada`, `Competicion`, `Equipo`, `FichaJugador`, `Jornada`, `Partido`, `PartidoParcial`, `PenalizacionClasificacion`.
- `DbSet<T>` correspondiente en `ApplicationDbContext` (mismo contexto que ya usa Identity, ver `## Aclaraciones`).
- Configuración Fluent API (`IEntityTypeConfiguration<T>`) para cada entidad: relaciones, restricciones únicas (`Competicion(TemporadaId, CategoriaId)`, `FichaJugador(EquipoId, Dorsal)`, `Jornada(CompeticionId, Numero)`), enums, y `RowVersion` como token de concurrencia en `Partido` y `Equipo`.
- Migración de EF Core (`dotnet ef migrations add`) que crea el esquema completo, aplicada y verificada contra el contenedor SQL local (`aspire run`).
- Test de integración que arranca el `AppHost` (`Aspire.Hosting.Testing`) y comprueba que la migración se aplica sin errores contra una base de datos real.

## Fuera de alcance

- Cualquier pantalla o endpoint de Admin o público (CRUD, listados, formularios) — próximos incrementos, uno por bloque de `screens.md`.
- Semillas de datos de catálogo (categorías estándar F.A.B., etc.) — se decide explícitamente dejarlas para cuando exista el CRUD de Admin, en vez de hardcodearlas en una migración (ver `## Aclaraciones`).
- Lógica de negocio: cálculo de la clasificación (`architecture.md` punto 14), marcador técnico automático en partidos resueltos, validaciones de aplazamiento/incomparecencia — el modelo deja los campos necesarios (`Partido.Estado`, `MotivoResolucion`, `PenalizacionClasificacion`...) pero el comportamiento se implementa junto con las pantallas que lo disparan.
- Exportación/importación JSON e iCal (`architecture.md` puntos 9 y 10) — dependen de este modelo pero son incrementos propios.

## Criterios de aceptación

- [x] Las once entidades de dominio de `data-model.md` existen como clases en `Data/` con su `DbSet<T>` en `ApplicationDbContext`.
- [x] Las restricciones únicas de `data-model.md` (`Competicion`, `FichaJugador`, `Jornada`) están declaradas vía Fluent API y se verifican con un intento de inserción duplicada en el test de integración.
- [x] `Partido` y `Equipo` tienen `RowVersion` como token de concurrencia optimista.
- [x] La migración de EF Core se genera y se aplica sin errores contra el contenedor SQL local (`aspire run`).
- [x] El test de integración que arranca el `AppHost` completo confirma que el esquema migra correctamente contra una base de datos real.
- [ ] La rama compila y todos los tests (unitarios + integración) pasan en CI.

## Aclaraciones

- **¿Mismo `ApplicationDbContext` que Identity, o uno separado para el dominio?** → Mismo `ApplicationDbContext`. Una sola base de datos, una sola cadena de migraciones, coherente con la decisión de `architecture.md` punto 2 de un único proyecto sin capas adicionales — no hay ninguna razón funcional para aislar Identity del dominio a este tamaño de proyecto, y separar añadiría dos DbContexts y dos historiales de migración sin beneficio claro.
- **¿Se siembran ya datos de catálogo (categorías estándar F.A.B.)?** → No en este incremento. Se deja el esquema listo pero vacío; las categorías reales se dan de alta a mano desde la pantalla de Admin correspondiente cuando exista, evitando hardcodear datos de negocio (nombres de categoría, orden) en una migración que tendría que revertirse o editarse si esos valores cambiasen.

## Referencias

- [[data-model]] — las once entidades que este incremento implementa.
- [[archive/BAS-3/spec|BAS-3]] — incremento del que depende (deja `ApplicationDbContext` e Identity funcionando).
- [[architecture#2. Organización del código|architecture.md, punto 2]] — un único proyecto, sin capas adicionales.
- [[architecture#3. Persistencia de datos|architecture.md, punto 3]] — EF Core como ORM.
