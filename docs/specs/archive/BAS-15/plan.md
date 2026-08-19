---
codigo: BAS-15
estado: Completado
tags:
  - plan
---

# BAS-15: Plan técnico

## Entidades del modelo de datos afectadas

- `Partido` (lectura/escritura, sin cambios de esquema) — `PuntosLocal`, `PuntosVisitante`, `EquipoGanadorResolucionId`, ya existentes.
- `PartidoParcial` (solo lectura) — se cargan los parciales de un partido para calcular la suma; sin cambios de esquema.

Ningún cambio de esquema — este incremento solo añade atajos de UI sobre datos ya existentes (ver `spec.md`, Fuera de alcance).

## Pantallas afectadas

- `Resultados` (`/Admin/Resultado/Edit/{id}`, `Areas/Admin/Pages/Resultado/Edit.cshtml` + `.cshtml.cs`) — única pantalla tocada. Sin pantallas nuevas.

## Decisiones técnicas específicas de este incremento

### Origen de la suma de parciales (sin recargar la página)

El criterio de aceptación exige que "Calcular desde los parciales" rellene el marcador sin recargar la página ni hacer una petición nueva. Como los parciales no se editan en esta misma pantalla (se gestionan en `Parciales`, pantalla aparte), la suma se puede precalcular una sola vez al cargar `Resultado/Edit` (`OnGetAsync`) y exponerla al cliente como atributos `data-*` en el propio botón — no hace falta AJAX ni un endpoint nuevo:

```csharp
// EditModel
public int SumaPuntosLocal { get; set; }
public int SumaPuntosVisitante { get; set; }
public bool TieneParciales { get; set; }
```

```csharp
var parciales = await context.PartidosParciales
    .Where(p => p.PartidoId == id)
    .Select(p => new { p.PuntosLocal, p.PuntosVisitante })
    .ToListAsync();
(SumaPuntosLocal, SumaPuntosVisitante) = ResultadoReglas.SumarParciales(
    parciales.Select(p => (p.PuntosLocal, p.PuntosVisitante)));
TieneParciales = parciales.Count > 0;
```

Nuevo método puro en `Domain/ResultadoReglas.cs` (mismo patrón que el ya existente `MarcadorTecnicoSugerido`, sin I/O, testeable de forma aislada):

```csharp
public static (int Local, int Visitante) SumarParciales(IEnumerable<(int PuntosLocal, int PuntosVisitante)> parciales) =>
    (parciales.Sum(p => p.PuntosLocal), parciales.Sum(p => p.PuntosVisitante));
```

Suma sin más — sin exigir que estén todos los periodos (aclaración de `spec.md`).

### Los tres botones (cliente, sin submit)

`<button type="button">` (no `type="submit"`) junto a los campos de marcador en `Resultado/Edit.cshtml`, con un pequeño script inline (mismo patrón que el ya existente para el desplegable de ganador, sin dependencia nueva de JS):

- **Técnica local**: `puntosLocal.value = 2; puntosVisitante.value = 0; ganadorSelect.value = <equipoLocalId>`.
- **Técnica visitante**: `puntosLocal.value = 0; puntosVisitante.value = 2; ganadorSelect.value = <equipoVisitanteId>`.
- **Calcular desde parciales** (`disabled` en el HTML si `!Model.TieneParciales`): `puntosLocal.value = sumaLocal; puntosVisitante.value = sumaVisitante`, y fija `ganadorSelect.value` al equipo con más puntos, o lo limpia (`""`) si `sumaLocal === sumaVisitante`.

`equipoLocalId`/`equipoVisitanteId` ya están disponibles vía `Model.PartidoContexto` — se añaden como `data-equipo-local-id`/`data-equipo-visitante-id` en el propio `ganadorSelect` (hoy solo lleva `data-equipo-local-id`, hace falta añadir el del visitante). `sumaLocal`/`sumaVisitante` como `data-suma-local`/`data-suma-visitante` en el botón de parciales.

### Se retira el autorrelleno inverso existente

El script actual de `Resultado/Edit.cshtml` escucha el `change` del `ganadorSelect` y reescribe el marcador (`ganadorSelect.addEventListener('change', ...)`, ver `archive/BAS-10/plan.md`). Se elimina ese listener — la relación pasa a ser de un solo sentido (botones de marcador → fijan ganador; cambiar el ganador a mano ya no toca el marcador), según la aclaración de `spec.md`.

### Bug preexistente encontrado en la verificación manual

`actualizarVisibilidad()` comparaba `estadoSelect.value === 'Jugado'`/`'Resuelto'`, pero `Html.GetEnumSelectList<PartidoEstado>()` genera el `value` de cada `<option>` como el entero subyacente del enum (`"1"`, `"4"`...), no el nombre — la comparación nunca era cierta y `grupoMarcador`/`grupoResolucion` no llegaban a mostrarse nunca al cambiar el `Estado` desde la interfaz, en ningún despliegue desde BAS-10. Corregido leyendo el texto de la opción seleccionada en vez de su valor:

```js
function estadoSeleccionadoTexto() {
    return estadoSelect.options[estadoSelect.selectedIndex].text;
}
```

Sin este arreglo, los tres botones nuevos de esta spec habrían quedado dentro de un contenedor que nunca se hace visible — es un prerrequisito real para que BAS-15 tenga efecto alguno, no un cambio de alcance aparte.

### Validación de servidor

Sin cambios: `ResultadoReglas.MarcadorValido`/`RequiereMarcador`/`ResolucionValida` siguen igual. Los tres botones son un atajo de relleno en el cliente, no alteran ni sustituyen la validación de servidor ya existente (criterio de aceptación explícito).

### Tests

- **Unitario** (`tests/BasketBaseTracker.Tests/Unit/ResultadoReglasTests.cs`): `SumarParciales` — suma varios parciales, lista vacía da `(0, 0)`, funciona con parciales de prórroga (`NumeroPeriodo` 5+, sin tratamiento especial, se suman igual).
- **Integración** (`tests/BasketBaseTracker.Tests/Integration/`, ampliando el fichero de resultados ya existente o uno nuevo): `GET /Admin/Resultado/Edit/{id}` con parciales ya cargados devuelve el HTML con los atributos `data-suma-local`/`data-suma-visitante` correctos y el botón de parciales **sin** `disabled`; sin parciales, el botón aparece `disabled`; los atributos `data-equipo-local-id`/`data-equipo-visitante-id` del desplegable de ganador son correctos.
- **Fuera de alcance de los tests automatizados**: el comportamiento de los botones en sí (clic → relleno de campos, sin recarga) es JavaScript puro en el cliente — la suite de integración actual (`HttpClient`, sin motor de JS) no lo ejecuta, y el smoke E2E (Playwright) está limitado a escenarios públicos de solo lectura (`architecture.md`, punto 15), no cubre el área Admin. Se verifica manualmente en local antes de cerrar el incremento.
