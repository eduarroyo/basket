---
codigo: BAS-8
titulo: Área Admin — plantilla de equipo (FichaJugador)
estado: Archivado
autor: Eduardo Arroyo
fechaCreacion: 2026-08-18
dependeDe:
  - "[[archive/BAS-7/spec|BAS-7]]"
tags:
  - backend
  - frontend
---

# BAS-8: Área Admin — plantilla de equipo (FichaJugador)

## Descripción

BAS-7 dejó gestionables los `Equipo`. Este incremento añade `Plantilla de equipo` de `screens.md`: alta, baja y edición de `FichaJugador` (dorsal, posición) — sin nombre ni ningún dato identificativo, por la decisión de no tratar datos personales ya recogida en `data-model.md`. A diferencia de las pantallas anteriores, `FichaJugador` no se gestiona como un listado global independiente: vive siempre dentro del contexto de un `Equipo` concreto ("plantilla de este equipo"), y es la primera entidad de la app con baja real en el alcance — `screens.md` dice explícitamente "Alta/baja/edición", a diferencia de las pantallas de BAS-6/BAS-7, y no hay ninguna otra entidad que referencie `FichaJugador` por clave foránea (nada que dejar huérfano al borrar).

## Alcance

- Pantalla `Plantilla` (`Areas/Admin/Pages/FichaJugador/Index.cshtml`, ruta `/Admin/FichaJugador/{equipoId}`) que lista las fichas de un equipo concreto, con su nombre de equipo como cabecera/contexto — no un listado global de todas las fichas de todos los equipos.
- Alta y edición de `FichaJugador` (dorsal, posición) dentro de ese contexto de equipo.
- Baja de `FichaJugador`, con página de confirmación (GET muestra los datos, POST borra) — primera pantalla de la app con esta acción.
- Enlace "Plantilla" desde el listado de `Equipos` (BAS-7) a la plantilla de cada equipo.
- Restricción única `(EquipoId, Dorsal)` (ya declarada en EF Core desde BAS-5) reflejada en la UI: un intento de alta/edición con un dorsal repetido dentro del mismo equipo muestra un error de validación en español.
- Desplegable de `Posicion` (nullable) con una opción en blanco, igual que `SedeHabitualId` de `Equipo` (BAS-7).
- Tests de integración: alta + edición + baja + listado, y un test que confirma que un dorsal duplicado dentro del mismo equipo se rechaza con un error de validación.

## Fuera de alcance

- Cualquier dato identificativo del jugador (nombre, fecha de nacimiento, etc.) — deliberadamente fuera del modelo por RGPD (`data-model.md`).
- `Calendario`, `Resultados`, `Clasificación`, `Penalizaciones de clasificación` — dependen de `Partido`, no de `FichaJugador`; incrementos futuros.
- Cualquier pantalla del área pública (ficha de equipo con su plantilla) — sigue sin datos de competición real que mostrar hasta que existan `Jornada`/`Partido`.
- Historial de cambios de dorsal o de bajas — `data-model.md` acepta explícitamente no rastrear cambios entre fichas.

## Criterios de aceptación

- [x] Un administrador autenticado puede ver, dar de alta, editar y dar de baja fichas de jugador dentro de la plantilla de un equipo concreto desde `/Admin`.
- [x] Dar de alta o editar una ficha con un dorsal ya usado en el mismo equipo muestra un error de validación en español y no guarda el cambio.
- [x] Dar de baja una ficha exige una confirmación explícita (pantalla de confirmación con los datos de la ficha) antes de borrarla.
- [x] Existe un test de integración que cubre alta + edición + baja + listado, y un test que verifica el rechazo del dorsal duplicado.
- [ ] La rama compila y todos los tests (unitarios + integración) pasan en CI.

## Aclaraciones

- **¿Por qué la plantilla no es un listado global de `FichaJugador` como las pantallas anteriores?** → `screens.md` la describe explícitamente como "dentro de un equipo" — una ficha sin el contexto de a qué equipo pertenece no tiene sentido de gestión por sí sola (el propio dorsal solo es único *dentro* de un equipo, no globalmente). Un listado global mezclaría fichas de equipos distintos sin ningún criterio útil de agrupación visual.
- **¿Por qué aquí sí hay borrado, a diferencia de BAS-6/BAS-7?** → Dos razones: `screens.md` lo pide explícitamente ("Alta/baja/edición", único sitio de "Gestión anual" que menciona baja hasta ahora), y no hay ningún riesgo de dejar datos huérfanos — ninguna otra entidad de `data-model.md` referencia `FichaJugador` por clave foránea, a diferencia de por qué se evitó el borrado en el catálogo (BAS-5 configuró esas relaciones en `Restrict`).

## Referencias

- [[screens]] — sección "Área admin", tabla "Gestión anual", fila `Plantilla de equipo`.
- [[data-model]] — entidad `FichaJugador`.
- [[archive/BAS-7/spec|BAS-7]] — incremento del que depende (`Equipo` ya gestionable).
