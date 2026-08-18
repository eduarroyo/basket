---
codigo: BAS-9
titulo: Área Admin — calendario de planificación (Jornada, Partido)
estado: Completado
autor: Eduardo Arroyo
fechaCreacion: 2026-08-18
dependeDe:
  - "[[archive/BAS-8/spec|BAS-8]]"
tags:
  - backend
  - frontend
---

# BAS-9: Área Admin — calendario de planificación (Jornada, Partido)

## Descripción

BAS-7 dejó gestionables las `Competicion` y sus `Equipo`. Este incremento añade `Calendario (planificación)` de `screens.md`: crear jornadas dentro de una competición y programar sus partidos (equipos, fecha/hora, sede) — sin entrar en resultados. `screens.md` separa deliberadamente `Calendario` de `Resultados` como dos pantallas distintas aunque compartan la entidad `Partido` ("planificación vs. resultado", ya recogido en `architecture.md`), así que este incremento solo toca los campos de planificación de `Partido` (equipos, sede, fecha/hora) y deja `Estado`, marcador, motivo de resolución y equipo ganador intactos para cuando llegue `Resultados` en un incremento futuro.

## Alcance

- Pantalla `Jornada` (`Areas/Admin/Pages/Jornada/`, ruta `/Admin/Jornada/{competicionId}`): listado, alta y edición de jornadas de una competición concreta — número, etiqueta opcional, si cuenta para la clasificación. Sin borrado (no lo pide `screens.md`, y una `Jornada` con partidos ya programados no podría borrarse sin arrastrarlos, `Restrict` desde BAS-5).
- Pantalla `Partido` (`Areas/Admin/Pages/Partido/`, ruta `/Admin/Partido/{jornadaId}`): listado, alta y edición de los partidos de una jornada concreta — equipo local, equipo visitante, sede (opcional, por defecto la sede habitual del local), fecha/hora (opcional, "aún no programado" si se deja en blanco). `Estado` se fija a `Programado` al crear y no es editable desde esta pantalla.
- Enlace "Calendario" desde `Competicion/Index` (BAS-7) a las jornadas de cada competición; enlace "Partidos" desde `Jornada/Index` a los partidos de cada jornada.
- Restricción única `(CompeticionId, Numero)` de `Jornada` (ya declarada en EF Core desde BAS-5) reflejada en la UI, mismo patrón que `Competicion`/`FichaJugador`.
- Invariante de aplicación de `Partido` (un equipo no puede aparecer dos veces en la misma jornada, ni como local ni como visitante — `data-model.md`, no expresable como restricción de base de datos) validada en el alta/edición, con un mensaje de error en español. Añadida también la comprobación de que un equipo no juegue contra sí mismo.
- Tests: unitario para la regla de "equipo repetido en la jornada" (lógica pura, sin I/O — primer test unitario de dominio de la app más allá de configuración), y de integración para alta + edición + listado de `Jornada` y `Partido`, más los rechazos correspondientes.

## Fuera de alcance

- `Estado`, `PuntosLocal`/`PuntosVisitante`, `MotivoResolucion`, `EquipoGanadorResolucionId`, `Observaciones`, `RowVersion` de `Partido` — pantalla `Resultados`, incremento futuro.
- `Clasificación` y `Penalizaciones de clasificación` — dependen de que existan partidos con resultado; incrementos futuros.
- Borrado de `Jornada` o `Partido`.
- Cualquier pantalla del área pública (calendario visible por el público) — sigue sin datos suficientes hasta que exista `Resultados`.
- Cuadro visual de eliminatorias — descartado explícitamente en `architecture.md` punto 16.

## Criterios de aceptación

- [x] Un administrador autenticado puede listar, dar de alta y editar jornadas de una competición, y partidos de una jornada (equipos, sede, fecha/hora) desde `/Admin`.
- [x] Dar de alta o editar una jornada con un número ya usado en la misma competición muestra un error de validación en español y no guarda el cambio.
- [x] Dar de alta o editar un partido con un equipo que ya juega en esa jornada (como local o visitante, en otro partido) muestra un error de validación en español y no guarda el cambio; lo mismo si el equipo local y el visitante son el mismo.
- [x] Editar un partido no modifica su `Estado` ni ningún campo de resultado, aunque no aparezcan en el formulario.
- [x] Existe un test unitario para la regla de equipo repetido en la jornada, y tests de integración de alta + edición + listado para `Jornada` y `Partido`, más los rechazos correspondientes.
- [x] La rama compila y todos los tests (unitarios + integración) pasan en CI.

## Aclaraciones

- **¿Por qué `Partido` no expone `Estado` ni el marcador en este incremento?** → `screens.md` ya separa `Calendario` (planificación) de `Resultados` como dos pantallas con propósitos distintos sobre la misma entidad. Mezclarlas ahora obligaría a decidir prematuramente el flujo de introducción de resultados (aplazamientos, incomparecencias, marcador técnico — reglamento FAB, ver `data-model.md`) sin que este incremento lo necesite.
- **¿Cómo se evita que editar un partido reinicie su `Estado` a `Programado` sin querer?** → El *handler* de edición carga la entidad ya existente y solo actualiza los campos que sí aparecen en el formulario (equipos, sede, fecha/hora) sobre esa misma instancia rastreada por EF Core, en vez del patrón `Attach` + `EntityState.Modified` usado en BAS-7/BAS-8 — ese patrón marca *todas* las propiedades como modificadas, y como el formulario no envía `Estado` ni el marcador, se sobrescribirían con sus valores por defecto. Detalle técnico en `plan.md`.
- **¿Por qué la validación de "equipo repetido en la jornada" es una consulta previa y no una restricción de base de datos capturada por excepción, a diferencia de `Competicion`/`FichaJugador`?** → Porque no existe ninguna restricción de base de datos que la exprese (`data-model.md` lo dice explícitamente) — no hay excepción que capturar. Se acepta la ventana de carrera teórica entre la comprobación y el guardado, igual que se aceptó para otras decisiones de simplicidad del proyecto, dado el volumen de administradores concurrentes.

## Referencias

- [[screens]] — sección "Área admin", tabla "Gestión anual", fila `Calendario (planificación)`.
- [[data-model]] — entidades `Jornada`, `Partido`.
- [[archive/BAS-7/spec|BAS-7]] — incremento del que depende (`Competicion` y `Equipo` ya gestionables).
- [[architecture#16. Formato de competición y fases finales|architecture.md, punto 16]] — `Etiqueta`/`CuentaParaClasificacion` de `Jornada`.
