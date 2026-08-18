---
codigo: BAS-9
estado: Completado
tags:
  - plan
---

# BAS-9: Plan técnico

## Entidades del modelo de datos afectadas

- `Jornada` — ya existe desde BAS-5 (solo esquema). Este incremento le añade `[ValidateNever]` a la navegación `Competicion` (mismo problema que `Competicion`/`Equipo`/`FichaJugador` en BAS-7/BAS-8: sin ella, la validación implícita de nullable reference types rechaza el alta/edición en silencio porque el formulario solo envía `CompeticionId`) y `[Required]`/`[StringLength]` a `Etiqueta`.
- `Partido` — ya existe desde BAS-5 (solo esquema). Este incremento le añade `[ValidateNever]` a `Jornada`, `EquipoLocal`, `EquipoVisitante`, `Sede` y `EquipoGanadorResolucion` (todas navegaciones no enviadas por el formulario de planificación). `Estado`, `PuntosLocal`/`PuntosVisitante`, `MotivoResolucion`, `EquipoGanadorResolucionId`, `Observaciones`, `RowVersion` no se tocan desde las páginas de este incremento (fuera de alcance) pero sí necesitan quedar protegidas del patrón `Attach`/`EntityState.Modified` — ver más abajo.
- No se toca `Equipo`, `Competicion`, `Sede`: solo se leen para poblar los desplegables de las páginas nuevas.

## Pantallas afectadas

- `screens.md`, área Admin, "Gestión anual" → `Calendario (planificación)`.
- Nuevas: `Areas/Admin/Pages/Jornada/{Index,Create,Edit}.cshtml(.cs)`, `Areas/Admin/Pages/Partido/{Index,Create,Edit}.cshtml(.cs)`.
- Modificadas: `Areas/Admin/Pages/Competicion/Index.cshtml` (enlace "Calendario" por fila), `Areas/Admin/Pages/Shared/_Layout.cshtml` (sin enlace de nivel superior — se navega desde `Competicion`, igual que `FichaJugador` se navega desde `Equipo` en BAS-8, no desde el menú principal).

## Decisiones técnicas específicas de este incremento

### Rutas

Mismo patrón de anidamiento por segmento de ruta que `FichaJugador` (BAS-8):

- `/Admin/Jornada/{competicionId}` — Index (jornadas de esa competición).
- `/Admin/Jornada/Create/{competicionId}` — alta.
- `/Admin/Jornada/Edit/{id}` — edición (la competición se deriva de la jornada cargada).
- `/Admin/Partido/{jornadaId}` — Index (partidos de esa jornada).
- `/Admin/Partido/Create/{jornadaId}` — alta.
- `/Admin/Partido/Edit/{id}` — edición (la jornada se deriva del partido cargado).

### Ámbito de los desplegables de equipo en `Partido`

`EquipoLocalId`/`EquipoVisitanteId` solo deben ofrecer equipos de la **misma competición** que la jornada (un partido entre equipos de competiciones distintas no tiene sentido y `data-model.md` no lo prohíbe explícitamente a nivel de FK porque `Equipo` ya cuelga de `Competicion`). El `OnGet`/`OnGetAsync` de Create/Edit filtra `context.Equipos.Where(e => e.CompeticionId == jornada.CompeticionId)` en vez de listar todos los equipos.

### Patrón "cargar y parchear" en `Partido/Edit`

A diferencia de `Competicion`/`Equipo`/`FichaJugador` (BAS-7/BAS-8), donde el formulario cubre *todas* las columnas de la entidad y el `Attach` + `EntityState.Modified` habitual es seguro, `Partido` tiene columnas (`Estado`, marcador, `MotivoResolucion`, `EquipoGanadorResolucionId`, `Observaciones`, `RowVersion`) que este formulario no envía y que pertenecerán a la futura pantalla `Resultados`. Si se marcara la entidad entera como modificada, EF Core sobrescribiría esas columnas con los valores por defecto del `Partido` recién deserializado del *model binding* (`Estado = Programado` siempre, marcador a `null`, etc.), perdiendo cualquier resultado ya introducido en el futuro.

En su lugar, `OnPostAsync` de `Partido/Edit` hace:

```csharp
var partido = await context.Partidos.FindAsync([Partido.Id], cancellationToken);
if (partido is null) return NotFound();

partido.JornadaId = Partido.JornadaId;
partido.EquipoLocalId = Partido.EquipoLocalId;
partido.EquipoVisitanteId = Partido.EquipoVisitanteId;
partido.SedeId = Partido.SedeId;
partido.FechaHora = Partido.FechaHora;

await context.SaveChangesAsync(cancellationToken);
```

`FindAsync` devuelve la instancia ya rastreada por el `DbContext` (o la adjunta si no estaba en el *change tracker*), así que solo las cinco propiedades asignadas explícitamente se marcan como modificadas — el resto conserva lo que hubiera en base de datos. Este patrón se documenta aquí porque es la primera vez que hace falta en el proyecto; los incrementos futuros que solo actualicen un subconjunto de columnas (como `Resultados` sobre este mismo `Partido`) deben replicarlo.

`Jornada/Edit` y el resto de páginas de este incremento sí cubren todas sus columnas y usan el patrón `Attach`/`EntityState.Modified` habitual.

### Regla de negocio pura: equipo repetido en la jornada

`data-model.md` señala explícitamente que "un equipo no puede aparecer dos veces en la misma jornada, ni como local ni como visitante" no es expresable como restricción de base de datos. Se implementa como método estático puro, sin dependencias de EF Core ni de la base de datos, para poder testearlo como unitario (`architecture.md`, punto 15):

```csharp
namespace BasketBaseTracker.Web.Domain;

public static class PartidoReglas
{
    public static bool EquipoYaJuegaEnJornada(
        IEnumerable<(int PartidoId, int EquipoLocalId, int EquipoVisitanteId)> partidosDeLaJornada,
        int partidoIdActual,
        int equipoLocalId,
        int equipoVisitanteId)
    {
        return partidosDeLaJornada.Any(p =>
            p.PartidoId != partidoIdActual &&
            (p.EquipoLocalId == equipoLocalId || p.EquipoVisitanteId == equipoLocalId ||
             p.EquipoLocalId == equipoVisitanteId || p.EquipoVisitanteId == equipoVisitanteId));
    }
}
```

`partidoIdActual` vale `0` en el alta (ningún partido existente tiene `Id = 0`), lo que permite reutilizar el mismo método en Create y Edit sin una sobrecarga separada. El *handler* de página consulta `context.Partidos.Where(p => p.JornadaId == ...).Select(p => new (p.Id, p.EquipoLocalId, p.EquipoVisitanteId)).ToListAsync()` y pasa el resultado al método puro; si devuelve `true`, o si `EquipoLocalId == EquipoVisitanteId`, se añade un `ModelState.AddModelError(string.Empty, "...")` en español, mismo patrón visual que el resto de restricciones únicas manejadas en la UI desde BAS-7.

Ubicación: `src/BasketBaseTracker.Web/Domain/PartidoReglas.cs` — primera carpeta `Domain/` del proyecto (hasta ahora todo el código no-UI vivía en `Data/`); se separa porque es lógica de negocio pura, no acceso a datos, distinción que `architecture.md` punto 15 ya anticipa para "validaciones de negocio (aplazamientos, incomparecencias, alineación indebida, fechas de jornada)".

### Test unitario

`tests/BasketBaseTracker.Tests/Unit/PartidoReglasTests.cs` — primer fichero de esa carpeta. Casos: sin partidos previos (falso), equipo local repetido como local de otro partido (verdadero), equipo local repetido como visitante de otro partido (verdadero), mismo `partidoIdActual` excluido correctamente (edición sin cambios no se marca a sí mismo como conflicto), equipo local == equipo visitante (se valida aparte, no por este método).

### Restricción única de `Jornada`

`(CompeticionId, Numero)`, ya declarada en `AppDbContext` desde BAS-5. Mismo patrón de captura de `DbUpdateException`/`SqlException { Number: 2601 or 2627 }` que `Competicion`/`FichaJugador` (BAS-7/BAS-8).
