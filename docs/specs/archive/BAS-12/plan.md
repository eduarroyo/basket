---
codigo: BAS-12
estado: Completado
tags:
  - plan
---

# BAS-12: Plan técnico

## Entidades del modelo de datos afectadas

- `Temporada`, `Competicion`, `Categoria`, `Equipo`, `Club`, `Sede`, `FichaJugador` — todas ya existen desde BAS-5/6/7/8, sin cambios de esquema. Este incremento solo las **consulta**.
- Sin migración EF Core.

## Pantallas afectadas

- `screens.md`, área pública → `Portada`, `Listado de competiciones`, `Ficha de equipo`, `Ficha de club`, `Ficha de sede`.

## Decisiones técnicas específicas de este incremento

### Rutas y ubicación de los ficheros

Mismo patrón que `Clasificacion.cshtml` (BAS-11): `@page` con ruta absoluta (empieza por `/`), que ignora la convención de carpetas del área `Public` ya configurada en `Program.cs`.

| Pantalla | Fichero | Ruta |
| --- | --- | --- |
| Portada | `Areas/Public/Pages/Index.cshtml` | `/` (ya existente, sin `@page` explícito adicional) |
| Listado de competiciones | `Areas/Public/Pages/Temporadas/Competiciones.cshtml` | `/temporadas/{id:int}/competiciones` |
| Ficha de equipo | `Areas/Public/Pages/Equipos/Index.cshtml` | `/equipos/{id:int}` |
| Ficha de club | `Areas/Public/Pages/Clubes/Index.cshtml` | `/clubes/{id:int}` |
| Ficha de sede | `Areas/Public/Pages/Sedes/Index.cshtml` | `/sedes/{id:int}` |

### Consultas

Sin lógica de negocio nueva (a diferencia de `ClasificacionCalculator`/`ResultadoReglas`/`PartidoReglas`): son lecturas directas con `Include`, así que no hace falta una capa de dominio intermedia — cada `PageModel` consulta `ApplicationDbContext` directamente, igual que las páginas admin de solo lectura.

- **Portada**: `context.Temporadas.OrderByDescending(t => t.FechaInicio).ToListAsync()`.
- **Listado de competiciones**: `context.Competiciones.Where(c => c.TemporadaId == id).Include(c => c.Categoria).Include(c => c.Temporada).Include("Equipos")` — como `Competicion` no tiene una colección de navegación `Equipos` (la relación es unidireccional, `Equipo.CompeticionId`, ver `data-model.md`), los equipos de cada competición se cargan con una segunda consulta `context.Equipos.Where(e => competicionIds.Contains(e.CompeticionId)).ToListAsync()` y se agrupan en memoria por `CompeticionId`, en vez de forzar una navegación inversa que ninguna otra pantalla necesita.
- **Ficha de equipo**: `context.Equipos.Include(e => e.Club).Include(e => e.SedeHabitual).Include(e => e.Competicion).ThenInclude(c => c.Temporada).Include(e => e.Competicion).ThenInclude(c => c.Categoria).FirstOrDefaultAsync(e => e.Id == id)`, más `context.FichasJugador.Where(f => f.EquipoId == id).OrderBy(f => f.Dorsal).ToListAsync()`.
- **Ficha de club**: `context.Clubes.FindAsync(id)`, más los equipos del club en la temporada `EnCurso`: `context.Equipos.Include(e => e.Competicion).ThenInclude(c => c.Temporada).Where(e => e.ClubId == id && e.Competicion.Temporada.Estado == TemporadaEstado.EnCurso).ToListAsync()` — lista vacía si no hay ninguna temporada `EnCurso`, sin caso especial adicional en el código (la propia consulta ya devuelve vacío).
- **Ficha de sede**: `context.Sedes.FindAsync(id)`, sin más consultas (sin "próximos partidos allí", fuera de alcance).

### Output Caching

Mismo `[OutputCache(PolicyName = "Publico")]` de BAS-11 sobre cada `PageModel`. Sin `VaryByRouteValueNames` explícito, por la misma razón que en BAS-11 (la ruta ya incluye el `id`, y el cacheo por defecto varía por URL completa).

### 404 en `id` inexistente

Todas las páginas devuelven `NotFound()` si la consulta principal no encuentra la entidad — mismo patrón que las páginas admin de edición.

### Tests

`tests/BasketBaseTracker.Tests/Integration/PublicPagesTests.cs`: una clase para las cinco páginas (comparten el mismo árbol de datos de partida — temporada, categoría, club, sede, competición, equipo, ficha de jugador — creado a través del área admin, como en el resto de tests de integración del proyecto). Casos: cada página muestra el contenido esperado y los enlaces de navegación correctos; `Ficha de club` sin temporada `EnCurso` no falla; `id` inexistente devuelve 404 en las cinco; Output Caching activo (cabecera `Age` en la segunda petición, igual que `ClasificacionPagesTests`, BAS-11).

### Hallazgo operativo (no bloqueante): contraseña de `sa` desincronizada en el volumen de datos local

Al intentar la verificación manual en navegador (`aspire run` local, no los tests — esos usan SQL efímero y no se ven afectados), el contenedor SQL Server arrancó pero `Web` nunca llegó a levantar: el log del contenedor (`docker logs`) mostraba `Login failed for user 'sa'. Reason: Password did not match that for the login provided.` en bucle. Causa: `AppHost.cs` usa `WithDataVolume()` para persistir los datos de SQL Server entre reinicios de `aspire run` (`dotnet` skill, `docs/architecture.md`) — el volumen (`basketbasetracker.apphost-0ce77125c8-sql-data`) conserva la contraseña de `sa` con la que se inicializó la primera vez, pero el parámetro `sql-admin-password` que genera Aspire para esta sesión era distinto, así que el contenedor (nuevo cada vez) nunca pudo autenticarse contra los datos ya existentes del volumen. No es un bug de esta spec ni de ningún incremento — es un problema del entorno local de desarrollo, ya presente antes de este incremento. No se ha resuelto (requeriría borrar el volumen, una acción destructiva sobre datos de desarrollo local que exige confirmación explícita del usuario) — queda anotado para cuando el usuario quiera revisarlo.
