---
codigo: BAS-13
estado: Completado
tags:
  - plan
---

# BAS-13: Plan técnico

## Entidades del modelo de datos afectadas

- `Jornada`, `Partido`, `PartidoParcial`, `Equipo`, `Sede` — todas ya existen desde BAS-5/9/10, sin cambios de esquema. Este incremento solo las **consulta**.
- Sin migración EF Core.

## Pantallas afectadas

- `screens.md`, área pública → `Calendario de competición`, `Detalle de partido`, `Resultados por jornada`, `Resultados por equipo`.
- Modificadas: `Areas/Public/Pages/Temporadas/Competiciones.cshtml` (BAS-12, enlace "Calendario" por competición), `Areas/Public/Pages/Equipos/Index.cshtml` (BAS-12, enlace "Resultados").

## Decisiones técnicas específicas de este incremento

### Rutas

Mismo patrón de ruta absoluta explícita que el resto del área pública (BAS-11/BAS-12).

| Pantalla | Fichero | Ruta |
| --- | --- | --- |
| Calendario de competición | `Areas/Public/Pages/Competiciones/Calendario.cshtml` | `/competiciones/{id:int}/calendario` |
| Resultados por jornada | `Areas/Public/Pages/Competiciones/Jornada.cshtml` | `/competiciones/{id:int}/jornadas/{n:int}` |
| Detalle de partido | `Areas/Public/Pages/Partidos/Index.cshtml` | `/partidos/{id:int}` |
| Resultados por equipo | `Areas/Public/Pages/Equipos/Resultados.cshtml` | `/equipos/{id:int}/resultados` |

`Resultados por jornada` direcciona por `(CompeticionId, Numero)`, no por el `Id` interno de `Jornada` — coincide con la ruta sugerida en `screens.md` (`/competiciones/{id}/jornadas/{n}`) y da una URL más significativa/estable que exponer el `Id` de base de datos. `Numero` es único solo dentro de una competición (restricción `(CompeticionId, Numero)`, BAS-9), así que la consulta necesita ambos valores: `context.Jornadas.FirstOrDefaultAsync(j => j.CompeticionId == id && j.Numero == n)`.

### Consultas

Sin lógica de negocio nueva, igual que BAS-12 — lecturas directas con `Include` desde cada `PageModel`.

- **Calendario de competición**: `context.Jornadas.Where(j => j.CompeticionId == id).OrderBy(j => j.Numero)`, más `context.Partidos.Where(p => jornadaIds.Contains(p.JornadaId)).Include(p => p.EquipoLocal).Include(p => p.EquipoVisitante).Include(p => p.Sede)`, agrupados en memoria por `JornadaId` (mismo patrón que `EquiposPorCompeticion` en BAS-12, `Jornada` tampoco tiene una colección de navegación `Partidos`).
- **Resultados por jornada**: la `Jornada` por `(CompeticionId, Numero)`, más sus partidos con marcador.
- **Detalle de partido**: `context.Partidos.Include(p => p.EquipoLocal).Include(p => p.EquipoVisitante).Include(p => p.Sede).Include(p => p.EquipoGanadorResolucion).Include(p => p.Jornada).ThenInclude(j => j.Competicion).ThenInclude(c => c.Temporada/Categoria)`, más `context.PartidosParciales.Where(pp => pp.PartidoId == id).OrderBy(pp => pp.NumeroPeriodo)`.
- **Resultados por equipo**: `context.Partidos.Where(p => p.EquipoLocalId == id || p.EquipoVisitanteId == id).Include(...).OrderByDescending(p => p.FechaHora)`. Sin filtro adicional por temporada — un `Equipo` ya está scopeado a una única competición/temporada por diseño (`data-model.md`), así que "histórico... en la temporada" ya es automático.

### Output Caching

Mismo `[OutputCache(PolicyName = "Publico")]` sobre cada `PageModel`, sin `VaryByRouteValueNames` (mismo razonamiento que BAS-11/BAS-12: la ruta ya incluye los identificadores, el cacheo por defecto ya varía por URL completa).

### 404

Todas las páginas devuelven `NotFound()` si la consulta principal no encuentra la entidad (o, en `Resultados por jornada`, si no existe una `Jornada` con ese `(CompeticionId, Numero)`).

### Tests

`tests/BasketBaseTracker.Tests/Integration/CalendarioYResultadosPublicosTests.cs`: mismo árbol de datos de partida que `PublicPagesTests` (BAS-12) y `CalendarioAdminPagesTests`/`ResultadoAdminPagesTests` (BAS-9/10) para crear jornadas y marcar partidos jugados. Casos: cada página muestra el contenido esperado (incluido un partido `Resuelto` con motivo/ganador en `Detalle de partido`) y los enlaces correctos; `n` inexistente en `Resultados por jornada` y `id` inexistente en las otras tres devuelven 404; Output Caching activo en las cuatro (cabecera `Age`, igual que BAS-11/BAS-12).
