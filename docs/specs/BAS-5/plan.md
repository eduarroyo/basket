---
codigo: BAS-5
estado: Planificado
tags:
  - plan
---

# BAS-5: Plan técnico

## Entidades del modelo de datos afectadas

Las once entidades de `data-model.md`, todas nuevas en el código (el esquema ya existía como documento, no como clases):

- Catálogo: `Categoria`, `Club`, `Sede`.
- Ancladas a temporada: `Temporada`, `Competicion`, `Equipo`, `FichaJugador`.
- Calendario y resultados: `Jornada`, `Partido`, `PartidoParcial`.
- Clasificación: `PenalizacionClasificacion` (la clasificación en sí sigue sin tabla propia, calculada — `architecture.md` punto 14, fuera de alcance de este incremento).

## Pantallas afectadas

Ninguna — incremento de esquema puro, sin páginas ni endpoints nuevos.

## Decisiones técnicas específicas de este incremento

### Estructura de ficheros

- `src/BasketBaseTracker.Web/Data/Entities/`: una clase por entidad (POCO, sin dependencia de EF Core más allá de los tipos).
- `src/BasketBaseTracker.Web/Data/Configurations/`: una clase `IEntityTypeConfiguration<T>` por entidad, aplicadas en `ApplicationDbContext.OnModelCreating` vía `modelBuilder.ApplyConfigurationsFromAssembly(...)`. Evita un `OnModelCreating` de cientos de líneas y mantiene cada entidad autocontenida — patrón estándar de EF Core, no una capa adicional.

### Enums almacenados como string

Todos los enums del modelo (`Temporada.Estado`, `Equipo.Estado`, `Partido.Estado`, `Partido.MotivoResolucion`, `FichaJugador.Posicion`) se mapean con `.HasConversion<string>()` en vez de dejarlos como `int` por defecto. Legible directamente en la base de datos (útil para depuración manual con SSMS/Azure Data Studio, dado que es un proyecto de una sola persona sin herramientas de administración adicionales) y evita el riesgo de reordenar valores del enum en el futuro y desincronizar silenciosamente los datos ya guardados. El coste (unos bytes más por fila, comparación por string en vez de int) es irrelevante al volumen de datos de este proyecto.

### Longitudes de string explícitas

Todas las propiedades `string` no acotadas por `data-model.md` (nombres, municipios, direcciones, motivos de texto libre) llevan `HasMaxLength` explícito en la configuración Fluent API en vez de quedar como `nvarchar(max)` por defecto — valores razonables por tipo de campo (p. ej. 200 para nombres, 500 para direcciones/observaciones/motivos de texto libre). Evita columnas sin límite por descuido; no se documenta cada valor aquí porque no es una decisión de arquitectura, son los propios ficheros de configuración la referencia.

### Claves foráneas y borrado

Todas las relaciones son `Restrict` (no `Cascade`) por defecto de EF Core al no declararse lo contrario — coherente con que este incremento no implementa todavía ninguna pantalla de borrado; se revisará caso a caso (p. ej. si borrar una `Jornada` debe arrastrar sus `Partido`) cuando se construyan las pantallas de Admin que lo disparan, no aquí.

### Migración única

Una sola migración (`AddDominioEntities` o nombre equivalente) que añade las once tablas de golpe, en vez de una por entidad — no hay ninguna de estas tablas desplegable de forma independiente y útil por sí sola (todas cuelgan de `Competicion`), así que trocear la migración no aportaría nada y solo complicaría el historial.

### Verificación

- Local: `aspire run` (contenedor SQL Server ya configurado en `AppHost.cs`) + `dotnet ef database update` desde `src/BasketBaseTracker.Web`, confirmando visualmente el esquema con una herramienta de cliente SQL.
- Automatizada: nuevo test de integración en `tests/BasketBaseTracker.Tests/Integration/` que usa `Aspire.Hosting.Testing` para levantar el `AppHost` completo (ya existe un test equivalente de BAS-3 para el health check de `Web`, mismo patrón) y comprueba que `dbContext.Database.MigrateAsync()` no lanza excepción, más un intento de inserción duplicada contra cada restricción única para confirmar que se aplicó correctamente.
