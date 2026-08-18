---
codigo: BAS-10
estado: Planificado
tags:
  - plan
---

# BAS-10: Plan técnico

## Entidades del modelo de datos afectadas

- `Partido` — ya existe desde BAS-5, con sus campos de resultado sin usar hasta ahora (BAS-9 solo tocó los de planificación). Sin cambios de esquema: `Estado`, `PuntosLocal`/`PuntosVisitante`, `MotivoResolucion`, `EquipoGanadorResolucionId`, `Observaciones`, `RowVersion` ya están declarados y configurados (`PartidoConfiguration.cs`) desde BAS-5.
- `PartidoParcial` — ya existe desde BAS-5. Este incremento añade una restricción única `(PartidoId, NumeroPeriodo)` nueva en `PartidoParcialConfiguration.cs`, así que hace falta una migración EF Core.

## Pantallas afectadas

- `screens.md`, área Admin, "Gestión anual" → `Resultados`.
- Nuevas: `Areas/Admin/Pages/Resultado/Edit.cshtml(.cs)`, `Areas/Admin/Pages/PartidoParcial/{Index,Create,Edit,Delete}.cshtml(.cs)`.
- Modificada: `Areas/Admin/Pages/Partido/Index.cshtml(.cs)` (BAS-9) — añade columna `Estado`/marcador y enlace "Resultado" por fila.

## Decisiones técnicas específicas de este incremento

### Rutas

- `/Admin/Resultado/Edit/{id}` — único *handler*, `id` = `PartidoId`. Sin `Create` (el partido ya existe desde `Calendario`) ni `Index` propio (ver spec.md, Aclaraciones).
- `/Admin/PartidoParcial/{partidoId}` — Index (parciales de ese partido).
- `/Admin/PartidoParcial/Create/{partidoId}` — alta.
- `/Admin/PartidoParcial/Edit/{id}` — edición.
- `/Admin/PartidoParcial/Delete/{id}` — baja con confirmación (mismo patrón que `FichaJugador`, BAS-8).

### `ResultadoReglas` — lógica de dominio pura

`src/BasketBaseTracker.Web/Domain/ResultadoReglas.cs`, junto a `PartidoReglas.cs` (BAS-9):

```csharp
public static class ResultadoReglas
{
    public static bool RequiereMarcador(PartidoEstado estado) =>
        estado is PartidoEstado.Jugado or PartidoEstado.Resuelto;

    public static bool MarcadorValido(int? puntosLocal, int? puntosVisitante) =>
        puntosLocal is not null && puntosVisitante is not null && puntosLocal != puntosVisitante;

    public static bool ResolucionValida(MotivoResolucion? motivo, int? equipoGanadorId, int equipoLocalId, int equipoVisitanteId) =>
        motivo is not null && equipoGanadorId is not null &&
        (equipoGanadorId == equipoLocalId || equipoGanadorId == equipoVisitanteId);

    public static (int Local, int Visitante) MarcadorTecnicoSugerido(int equipoGanadorId, int equipoLocalId) =>
        equipoGanadorId == equipoLocalId ? (2, 0) : (0, 2);
}
```

`MarcadorValido` rechaza el empate porque las reglas FIBA de prórroga lo excluyen siempre (`reglamento/resumen-reglas-relevantes.md`, §1) — no es una regla inventada para la aplicación. `ResolucionValida` no valida que `motivo`/`equipoGanadorId` sean coherentes entre sí más allá de "el ganador es uno de los dos equipos"; la distinción de intencionalidad (Art. 43 vs. 44) es una decisión humana del Juez Único de Competición, no algo que la aplicación pueda derivar (`spec.md`, Aclaraciones).

### `Resultado/Edit` — cargar y parchear + `RowVersion`

Combina el patrón "cargar y parchear" de `Partido/Edit` (BAS-9, evita tocar columnas de planificación) con la verificación de concurrencia optimista de `Equipo/Edit` (BAS-7). La diferencia frente a `Equipo/Edit` es que aquí la entidad no llega de un grafo desconectado (`Attach`), sino de una consulta ya trackeada en el mismo `DbContext` — así que fijar el valor original de `RowVersion` para que EF Core lo use en la cláusula `WHERE` de la actualización requiere tocar `ChangeTracker` explícitamente en vez de depender del valor que ya trae la entidad recién cargada (que, al venir de la misma consulta, *siempre* coincidiría con el de base de datos y nunca dispararía el conflicto):

```csharp
var partidoExistente = await context.Partidos.FindAsync([id], cancellationToken);
if (partidoExistente is null) return NotFound();

// ... validar con ResultadoReglas ...

partidoExistente.Estado = Partido.Estado;
partidoExistente.PuntosLocal = requiereMarcador ? Partido.PuntosLocal : null;
partidoExistente.PuntosVisitante = requiereMarcador ? Partido.PuntosVisitante : null;
partidoExistente.MotivoResolucion = esResuelto ? Partido.MotivoResolucion : null;
partidoExistente.EquipoGanadorResolucionId = esResuelto ? Partido.EquipoGanadorResolucionId : null;
partidoExistente.Observaciones = Partido.Observaciones;

context.Entry(partidoExistente).Property(p => p.RowVersion).OriginalValue = Partido.RowVersion;

try
{
    await context.SaveChangesAsync(cancellationToken);
}
catch (DbUpdateConcurrencyException)
{
    ModelState.AddModelError(string.Empty, "Este partido se ha modificado en otro sitio mientras tanto.");
    // recargar y volver a mostrar la página
}
```

Sin ese `OriginalValue` explícito, el `UPDATE` generado por EF Core no incluiría `RowVersion` en su `WHERE` con el valor que el administrador tenía al abrir el formulario, y el conflicto de concurrencia nunca se detectaría — la entidad ya viene "fresca" de la base de datos en el propio `OnPostAsync`, no del `HttpContext` de una petición anterior como en `Equipo/Edit`.

### Marcador técnico sugerido (UI, no dominio)

Cuando el administrador selecciona `Estado = Resuelto` y un equipo ganador, un pequeño script (`<script>` inline, sin dependencia nueva de JS) rellena `PuntosLocal`/`PuntosVisitante` con el resultado de `MarcadorTecnicoSugerido` — solo una sugerencia de UI, el campo sigue siendo editable y no se revalida contra ese valor concreto en el servidor (solo contra `MarcadorValido`, que únicamente exige que no haya empate).

### Restricción única de `PartidoParcial`

```csharp
builder.HasIndex(p => new { p.PartidoId, p.NumeroPeriodo }).IsUnique();
```

Mismo patrón de captura de `DbUpdateException`/`SqlException { Number: 2601 or 2627 }` que el resto de restricciones únicas del proyecto (`Jornada`, `Competicion`, `FichaJugador`). Requiere `dotnet ef migrations add AddPartidoParcialIndiceUnico --project src/BasketBaseTracker.Web`.

### Tests unitarios

`tests/BasketBaseTracker.Tests/Unit/ResultadoReglasTests.cs`: `MarcadorValido` con empate (falso), con marcador válido (verdadero), con algún punto nulo (falso); `ResolucionValida` con motivo y ganador válidos (verdadero), sin motivo (falso), con ganador que no es ninguno de los dos equipos (falso); `MarcadorTecnicoSugerido` devuelve `(2,0)` si gana el local y `(0,2)` si gana el visitante.

### Bug encontrado: extracción de `RowVersion` sin decodificar entidades HTML (afectaba también a BAS-7)

Al escribir el test de conflicto de concurrencia de `Resultado/Edit`, apareció un fallo intermitente **solo** al ejecutar la clase completa (nunca en aislamiento): el primer guardado, con un `RowVersion` recién leído del formulario, disparaba igualmente `DbUpdateConcurrencyException`.

Causa raíz: Razor codifica el `+` del Base64 de un `byte[]` como entidad HTML (`&#x2B;`) al escribir el atributo `value` de un campo oculto — el resto de tests de este incremento no lo sufrían porque su valor de `RowVersion` no contenía casualmente ningún carácter sujeto a esa codificación, pero el de `Partido/Edit` (BAS-9)/`Resultado/Edit` sí en una ejecución con el resto de la clase en paralelo (el valor concreto de `RowVersion` de cada partido depende del orden de escritura, distinto en cada ejecución). Los tests de integración leían el atributo `value="..."` directamente con una expresión regular, sin decodificar esa entidad, y reenviaban el texto codificado tal cual como si fuera el propio valor — un `RowVersion` corrompido que nunca coincide con el de base de datos, así que el conflicto de concurrencia saltaba siempre que el valor real contuviera ese carácter, con independencia de que hubiera un conflicto real.

El mismo patrón de extracción (sin decodificar) ya existía desde BAS-7 en `GestionAnualAdminPagesTests.EquipoAltaListadoEdicionYConflictoDeConcurrencia` — probablemente el mismo bug latente, enmascarado hasta ahora por no haber tocado nunca un `RowVersion` con ese carácter en concreto.

Arreglado con un helper compartido nuevo, `AdminHttpTestHelpers.ExtraerCampoOculto(html, nombreDeCampo)`, que decodifica con `WebUtility.HtmlDecode` antes de devolver el valor — usado ahora tanto en `ResultadoAdminPagesTests` como en el `Equipo.RowVersion` de `GestionAnualAdminPagesTests`.
