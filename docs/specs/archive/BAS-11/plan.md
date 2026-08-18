---
codigo: BAS-11
estado: Completado
tags:
  - plan
---

# BAS-11: Plan técnico

## Entidades del modelo de datos afectadas

- `Partido`, `Equipo`, `PenalizacionClasificacion` — ya existen desde BAS-5/BAS-9/BAS-10, sin cambios de esquema. Este incremento solo las **consulta**, no las modifica.
- Sin migración EF Core.

## Pantallas afectadas

- `screens.md`, área pública → `Clasificación` (`/competiciones/{id}/clasificacion`).
- `screens.md`, área admin, "Gestión anual" → `Clasificación` (`/Admin/Clasificacion/{competicionId}`).
- Modificada: `Areas/Admin/Pages/Competicion/Index.cshtml` (enlace "Clasificación" por fila, junto al ya existente "Calendario").

## Decisiones técnicas específicas de este incremento

### `ClasificacionCalculator` — lógica de agregación pura

`src/BasketBaseTracker.Web/Domain/ClasificacionCalculator.cs`:

```csharp
public readonly record struct FilaClasificacion(
    int EquipoId,
    int PartidosJugados,
    int Victorias,
    int Derrotas,
    int PuntosFavor,
    int PuntosContra,
    int PuntosClasificacion)
{
    public int DiferenciaDeTantos => PuntosFavor - PuntosContra;

    public double CocienteDeTantos =>
        PuntosContra == 0 ? (PuntosFavor > 0 ? double.PositiveInfinity : 0) : (double)PuntosFavor / PuntosContra;
}

public static class ClasificacionCalculator
{
    public static IReadOnlyList<FilaClasificacion> Calcular(
        IEnumerable<int> equipoIds,
        IEnumerable<(int EquipoLocalId, int EquipoVisitanteId, int PuntosLocal, int PuntosVisitante)> partidos,
        IEnumerable<(int EquipoId, int Puntos)> penalizaciones,
        int puntosVictoria,
        int puntosDerrota)
    {
        var partidosList = partidos.ToList();
        var penalizacionesPorEquipo = penalizaciones
            .GroupBy(p => p.EquipoId)
            .ToDictionary(g => g.Key, g => g.Sum(p => p.Puntos));

        return equipoIds
            .Select(equipoId => CalcularFila(equipoId, partidosList, penalizacionesPorEquipo, puntosVictoria, puntosDerrota))
            .OrderByDescending(f => f.PuntosClasificacion)
            .ThenByDescending(f => f.DiferenciaDeTantos)
            .ThenByDescending(f => f.CocienteDeTantos)
            .ToList();
    }

    private static FilaClasificacion CalcularFila(
        int equipoId,
        List<(int EquipoLocalId, int EquipoVisitanteId, int PuntosLocal, int PuntosVisitante)> partidos,
        Dictionary<int, int> penalizacionesPorEquipo,
        int puntosVictoria,
        int puntosDerrota)
    {
        var jugados = partidos
            .Where(p => p.EquipoLocalId == equipoId || p.EquipoVisitanteId == equipoId)
            .Select(p => p.EquipoLocalId == equipoId
                ? (Favor: p.PuntosLocal, Contra: p.PuntosVisitante)
                : (Favor: p.PuntosVisitante, Contra: p.PuntosLocal))
            .ToList();

        var victorias = jugados.Count(j => j.Favor > j.Contra);
        var derrotas = jugados.Count - victorias;
        var puntosClasificacion = victorias * puntosVictoria + derrotas * puntosDerrota
            + penalizacionesPorEquipo.GetValueOrDefault(equipoId);

        return new FilaClasificacion(
            equipoId, jugados.Count, victorias, derrotas,
            jugados.Sum(j => j.Favor), jugados.Sum(j => j.Contra), puntosClasificacion);
    }
}
```

`CocienteDeTantos` evita `NaN`/excepción cuando un equipo no ha jugado ningún partido (`0/0`): se trata como `0`, para que quede por detrás de cualquier equipo con cociente positivo en el desempate, en vez de romper el `OrderByDescending`.

### `ClasificacionService` — consulta EF Core + ensamblado para la vista

`src/BasketBaseTracker.Web/Domain/ClasificacionService.cs`, registrado como `AddScoped<ClasificacionService>()` en `Program.cs`:

```csharp
public sealed record FilaClasificacionVista(
    int Posicion, string EquipoNombre, int PartidosJugados, int Victorias, int Derrotas,
    int PuntosFavor, int PuntosContra, int DiferenciaDeTantos, int PuntosClasificacion);

public class ClasificacionService(ApplicationDbContext context)
{
    public async Task<IReadOnlyList<FilaClasificacionVista>?> ObtenerAsync(int competicionId, CancellationToken cancellationToken)
    {
        var competicion = await context.Competiciones.FindAsync([competicionId], cancellationToken);
        if (competicion is null) return null;

        var equipos = await context.Equipos
            .Where(e => e.CompeticionId == competicionId)
            .Select(e => new { e.Id, e.Nombre })
            .ToListAsync(cancellationToken);

        var partidos = await context.Partidos
            .Where(p => p.Jornada.CompeticionId == competicionId
                && p.Jornada.CuentaParaClasificacion
                && (p.Estado == PartidoEstado.Jugado || p.Estado == PartidoEstado.Resuelto))
            .Select(p => new { p.EquipoLocalId, p.EquipoVisitanteId, PuntosLocal = p.PuntosLocal!.Value, PuntosVisitante = p.PuntosVisitante!.Value })
            .ToListAsync(cancellationToken);

        var equipoIds = equipos.Select(e => e.Id).ToList();
        var penalizaciones = await context.PenalizacionesClasificacion
            .Where(p => equipoIds.Contains(p.EquipoId))
            .Select(p => new { p.EquipoId, p.Puntos })
            .ToListAsync(cancellationToken);

        var filas = ClasificacionCalculator.Calcular(
            equipoIds,
            partidos.Select(p => (p.EquipoLocalId, p.EquipoVisitanteId, p.PuntosLocal, p.PuntosVisitante)),
            penalizaciones.Select(p => (p.EquipoId, p.Puntos)),
            competicion.PuntosVictoria, competicion.PuntosDerrota);

        var nombresPorId = equipos.ToDictionary(e => e.Id, e => e.Nombre);
        return filas
            .Select((f, indice) => new FilaClasificacionVista(
                indice + 1, nombresPorId[f.EquipoId], f.PartidosJugados, f.Victorias, f.Derrotas,
                f.PuntosFavor, f.PuntosContra, f.DiferenciaDeTantos, f.PuntosClasificacion))
            .ToList();
    }
}
```

`PuntosLocal!.Value`/`PuntosVisitante!.Value` son seguros porque el filtro de `Estado` (`Jugado`/`Resuelto`) ya garantiza que `ResultadoReglas.RequiereMarcador` los exige no nulos (BAS-10) — el modelo de datos los deja `nullable` porque `Programado`/`Aplazado`/`Cancelado` no los tienen, no porque puedan faltar en estos dos estados.

Se usa `IReadOnlyList<...>? ` (`null` si la competición no existe) en vez de lanzar, para que ambas páginas puedan devolver `NotFound()` sin duplicar la comprobación de existencia dos veces.

### Rutas

- Pública: `@page "/competiciones/{id:int}/clasificacion"` en `Areas/Public/Pages/Competiciones/Clasificacion.cshtml` — ruta absoluta explícita (empieza por `/`), así que ignora la convención de carpetas del área `Public` ya configurada en `Program.cs` y coincide exactamente con `screens.md`.
- Admin: `/Admin/Clasificacion/{competicionId}`, mismo patrón de ruta por segmento que `Jornada`/`Partido` (BAS-9).

### Output Caching

`Program.cs`:

```csharp
builder.Services.AddOutputCache(options =>
{
    options.AddPolicy("Publico", policy => policy.Expire(TimeSpan.FromMinutes(4)));
});
```

```csharp
app.UseOutputCache(); // después de UseAuthorization(), antes de MapRazorPages()
```

La página pública lleva `[OutputCache(PolicyName = "Publico")]` sobre el `PageModel`. Sin política base global, así que ningún otro endpoint (incluida toda el área Admin) se cachea salvo que la use explícitamente — coincide con la intención de `architecture.md` punto 5 sin necesitar exclusión aparte. No hace falta `VaryByRouteValueNames`: el cacheo por defecto ya varía por URL completa, y la ruta ya incluye `competicionId` (`architecture.md` punto 5, ya lo señala explícitamente).

### Tests

- `tests/BasketBaseTracker.Tests/Unit/ClasificacionCalculatorTests.cs`: orden por puntos; empate resuelto por diferencia de tantos; empate resuelto por cociente cuando la diferencia también coincide; penalización resta puntos; equipo sin partidos aparece con todo a cero y cociente `0` (no `NaN`, no excepción).
- `tests/BasketBaseTracker.Tests/Integration/ClasificacionPagesTests.cs`: ambas páginas muestran la tabla esperada tras jugar varios partidos; una jornada con `CuentaParaClasificacion = false` no cuenta; cabecera `Cache-Control`/comportamiento de caché presente en la pública y ausente en la admin. No se testea el efecto de `PenalizacionClasificacion` a nivel HTTP (no hay UI para crearla todavía; ya lo cubre el unitario de `ClasificacionCalculator` sin necesidad de manipular la base de datos a mano en un test de integración).
