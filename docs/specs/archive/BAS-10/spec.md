---
codigo: BAS-10
titulo: Área Admin — resultados (marcador, parciales, estado administrativo)
estado: Completado
autor: Eduardo Arroyo
fechaCreacion: 2026-08-18
dependeDe:
  - "[[archive/BAS-9/spec|BAS-9]]"
tags:
  - backend
  - frontend
---

# BAS-10: Área Admin — resultados (marcador, parciales, estado administrativo)

## Descripción

BAS-9 dejó gestionable el calendario de planificación (`Jornada`, `Partido`) sin tocar `Estado` ni el marcador — deliberadamente, para no mezclar "planificación" y "resultado" (`screens.md`). Este incremento añade la pantalla `Resultados`: cambiar el `Estado` de un partido (`Jugado`, `Aplazado`, `Cancelado`, `Resuelto`, o revertir a `Programado`), introducir su marcador y, cuando `Estado = Resuelto`, el motivo y el equipo ganador de una resolución administrativa (incomparecencia, alineación indebida u otro motivo, con el marcador técnico 2-0 de `reglamento/resumen-reglas-relevantes.md` sugerido por defecto y editable). Añade también la gestión de `PartidoParcial` (marcador por periodo) de un partido.

## Alcance

- Página `Resultado` (`Areas/Admin/Pages/Resultado/Edit.cshtml`, ruta `/Admin/Resultado/Edit/{id}`, `id` = `PartidoId`): edita `Estado`, `PuntosLocal`/`PuntosVisitante`, `MotivoResolucion`, `EquipoGanadorResolucionId` y `Observaciones` de un partido ya planificado (BAS-9). No toca `EquipoLocalId`/`EquipoVisitanteId`/`SedeId`/`FechaHora` — siguen siendo terreno de `Calendario` (BAS-9).
- Sin pantalla de listado propia: se llega desde `Partido/Index` (BAS-9), que gana una columna de `Estado`/marcador y un enlace "Resultado" por fila — mismo patrón que "Plantilla" colgando de `Equipo/Index` en BAS-8, para no duplicar un listado casi idéntico al que ya existe.
- Reglas de validación, en `src/BasketBaseTracker.Web/Domain/ResultadoReglas.cs` (lógica pura, testeada como unitaria, `architecture.md` punto 15):
  - `Estado = Jugado` o `Resuelto` exige marcador, y el marcador no puede terminar en empate (las reglas FIBA de prórroga excluyen el empate — `reglamento/resumen-reglas-relevantes.md`, §1).
  - `Estado = Resuelto` exige además `MotivoResolucion` y `EquipoGanadorResolucionId`, y este último debe ser uno de los dos equipos del partido.
  - Al guardar con `Estado` distinto de `Jugado`/`Resuelto` (p. ej. al revertir a `Programado` o `Aplazado`, escenario del Art. 44 ya anticipado en `data-model.md`), se limpian `PuntosLocal`/`PuntosVisitante`/`MotivoResolucion`/`EquipoGanadorResolucionId` para no dejar un marcador obsoleto asociado a un partido que ya no está jugado ni resuelto.
- Al seleccionar `Estado = Resuelto` y un equipo ganador, la página sugiere 2-0 a favor de ese equipo como marcador por defecto (editable por el administrador, tal como indica `data-model.md`).
- Concurrencia optimista (`Partido.RowVersion`) sobre esta edición — primer uso del patrón combinado "cargar y parchear" (BAS-9) + verificación de `RowVersion` (BAS-7), porque esta pantalla solo debe fallar por conflicto si otro administrador tocó específicamente el resultado, no si BAS-9 tocó mientras tanto los campos de planificación que esta pantalla no lee.
- Pantallas `PartidoParcial` (`Areas/Admin/Pages/PartidoParcial/{Index,Create,Edit,Delete}`, ruta `/Admin/PartidoParcial/{partidoId}`): alta/edición/baja del marcador por periodo de un partido — mismo patrón de anidamiento y de páginas que `FichaJugador` (BAS-8), incluida página de confirmación de baja.
- Restricción única `(PartidoId, NumeroPeriodo)` de `PartidoParcial`, nueva en este incremento (no existía en el esquema de BAS-5) — evita dos filas para el mismo periodo del mismo partido.
- Tests: unitarios para `ResultadoReglas` (empate rechazado, motivo/ganador exigidos solo si `Resuelto`, ganador debe ser uno de los dos equipos, limpieza de marcador al revertir estado), de integración para el flujo completo de `Resultado` (marcar jugado, resolver administrativamente, revertir a programado, conflicto de concurrencia) y de `PartidoParcial` (alta, edición, baja, restricción única).

## Fuera de alcance

- `Clasificación` y `Penalizaciones de clasificación` — dependen de que existan partidos con resultado, pero son incrementos separados (`architecture.md` punto 14, nota de secuenciación; `screens.md` marca `Penalizaciones` como de las últimas pantallas).
- Cualquier pantalla del área pública (calendario/resultados/clasificación visibles al público) — sigue sin datos suficientes hasta que exista `Clasificación`.
- Descuento automático de 1 punto de clasificación por incomparecencia/alineación indebida con mala fe (Art. 43 del Reglamento Disciplinario) — se registra por separado, a mano, en la futura pantalla `Penalizaciones de clasificación`; esta pantalla no la dispara automáticamente.
- Distinguir en la UI alineación indebida "con mala fe" de "sin mala fe" (Art. 43.F vs. Art. 44) — ya resuelto en `data-model.md`: el caso sin mala fe se registra revirtiendo `Estado` a `Programado`/`Aplazado` y dejando constancia en `Observaciones`, sin un valor de `MotivoResolucion` dedicado; esta pantalla ya soporta ese flujo sin necesitar nada adicional.
- Validar que la suma de los parciales de `PartidoParcial` coincida con `PuntosLocal`/`PuntosVisitante` del partido — añadiría una comprobación cruzada no pedida por `functional.md`/`data-model.md`, y el administrador puede introducir parciales sin llevar cuenta exacta si no le interesa ese nivel de detalle.
- Borrado de `Partido` (ya fuera de alcance desde BAS-9).

## Criterios de aceptación

- [x] Un administrador autenticado puede cambiar el `Estado` de un partido planificado a `Jugado` (con marcador), `Resuelto` (con motivo, ganador y marcador técnico sugerido), `Aplazado` o `Cancelado`, y revertirlo a `Programado`, desde `/Admin/Resultado/Edit/{id}`.
- [x] Guardar `Estado = Jugado` o `Resuelto` sin marcador, o con marcador empatado, muestra un error de validación en español y no guarda el cambio.
- [x] Guardar `Estado = Resuelto` sin `MotivoResolucion`, sin `EquipoGanadorResolucionId`, o con un ganador que no es ninguno de los dos equipos del partido, muestra un error de validación en español y no guarda el cambio.
- [x] Revertir un partido a `Programado` o `Aplazado` limpia el marcador, el motivo y el ganador previamente guardados.
- [x] Dos administradores editando el resultado del mismo partido en paralelo: el segundo guardado muestra un error de concurrencia en español y no sobrescribe el del primero.
- [x] Un administrador puede dar de alta, editar y dar de baja (con confirmación) los parciales por periodo de un partido; dar de alta un periodo ya existente para ese partido muestra un error de validación en español.
- [x] Existen tests unitarios para `ResultadoReglas` y tests de integración para `Resultado` y `PartidoParcial`, y todos pasan en CI.

## Aclaraciones

- **¿Por qué no hay una pantalla de listado `Resultado/Index` propia?** → Sería casi idéntica a `Partido/Index` (BAS-9), que ya lista los partidos de una jornada. Se añade a esa página una columna de `Estado`/marcador y un enlace "Resultado" por fila, igual que BAS-8 colgó "Plantilla" de `Equipo/Index` en vez de crear un listado de equipos paralelo.
- **¿Por qué combinar "cargar y parchear" con la verificación de `RowVersion`, en vez de reusar tal cual el patrón de `Equipo/Edit` (BAS-7)?** → `Equipo/Edit` cubre todas las columnas de `Equipo` en su formulario, así que `Attach` + `EntityState.Modified` es seguro. `Partido` tiene columnas que pertenecen a `Calendario` (BAS-9) que este formulario no envía; si se usara `Attach` sin más, un conflicto de concurrencia saltaría también cuando otro administrador solo hubiera tocado la fecha o la sede del partido, algo que a esta pantalla no le importa. La solución técnica exacta (fijar `OriginalValue` de `RowVersion` sobre la entidad recién cargada, en vez de sobre un grafo desconectado) se documenta en `plan.md`.
- **¿Por qué el marcador técnico sugerido es siempre 2-0 y no reproduce las variantes de los Art. 80/148/149.2 (que a veces son 2-0 "si le fuera desfavorable" y a veces se respeta el marcador en curso)?** → Esos artículos regulan qué marcador *puede* proponer el reglamento según cómo se interrumpió el partido, pero la decisión final es del Juez Único de Competición, no automática. La pantalla sugiere el caso más común (2-0 a favor del ganador) como punto de partida editable, sin modelar la casuística completa — el administrador ajusta el número si el caso concreto lo pide, igual que ya hace con la sede o la fecha en otras pantallas.
- **¿Por qué una restricción única `(PartidoId, NumeroPeriodo)` en `PartidoParcial` si `data-model.md` no la menciona?** → `data-model.md` no la declaraba explícitamente, pero dos filas para el mismo periodo del mismo partido no tiene ningún significado válido; se añade ahora que existe una pantalla de alta que podría, si no, crear duplicados por error. Mismo patrón ya usado para `Jornada`/`FichaJugador`/`Competicion`.

## Referencias

- [[screens]] — sección "Área admin", tabla "Gestión anual", fila `Resultados`.
- [[data-model]] — entidades `Partido` (campos de resultado) y `PartidoParcial`.
- [[archive/BAS-9/spec|BAS-9]] — incremento del que depende (`Jornada`/`Partido` ya planificables).
- [[reglamento/resumen-reglas-relevantes#4-marcador-técnico-y-resultados-administrativos-fab|resumen-reglas-relevantes.md, §4]] — marcador técnico 2-0.
- [[architecture#14. Clasificación como consulta calculada|architecture.md, punto 14]] — nota de secuenciación de `PenalizacionClasificacion`.
