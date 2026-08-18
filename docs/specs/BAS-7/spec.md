---
codigo: BAS-7
titulo: Área Admin — gestión anual (Competiciones y Equipos)
estado: Planificado
autor: Eduardo Arroyo
fechaCreacion: 2026-08-18
dependeDe:
  - "[[archive/BAS-6/spec|BAS-6]]"
tags:
  - backend
  - frontend
---

# BAS-7: Área Admin — gestión anual (Competiciones y Equipos)

## Descripción

BAS-6 dejó el catálogo (Temporadas, Categorías, Clubes, Sedes) gestionable desde el Admin. Este incremento añade las dos primeras pantallas de "Gestión anual" de `screens.md`: `Competiciones` y `Equipos`, las siguientes en el orden natural porque son las primeras que referencian catálogo mediante claves foráneas — una `Competicion` cuelga de `Temporada` + `Categoria`, y un `Equipo` de `Competicion` + `Club` (+ opcionalmente `Sede`). Sin catálogo ya no habría nada que seleccionar en los desplegables; con él, ya es posible dar de alta una competición real y sus equipos participantes.

## Alcance

- Listado, alta y edición (sin borrado, mismo criterio que BAS-6) para `Competicion` y `Equipo`, en `Areas/Admin/Pages/<Entidad>/`.
- Desplegables en los formularios: `Competicion` (Temporada, Categoría); `Equipo` (Competición, Club, Sede habitual — esta última opcional, con opción "Sin sede habitual").
- El desplegable de Competición en el formulario de `Equipo` combina temporada y categoría en el texto mostrado (p. ej. "2025-2026 — Cadete"), porque `Competicion` no tiene un nombre propio de que tirar.
- Restricción única `(TemporadaId, CategoriaId)` de `Competicion` (ya declarada en EF Core desde BAS-5) reflejada en la UI: un intento de alta/edición duplicada muestra un error de validación en español, no una excepción sin capturar.
- Concurrencia optimista de `Equipo` (`RowVersion`, BAS-5): el formulario de edición la incluye como campo oculto y captura `DbUpdateConcurrencyException` con un mensaje en español — primera entidad de la app que ejercita esto de verdad.
- Tests de integración: alta + edición + listado por entidad, más un test que confirma que el alta duplicada de `Competicion` (misma temporada+categoría) se rechaza con un error de validación, no con un 500.

## Fuera de alcance

- Borrado — igual que en BAS-6, ninguna pantalla de `screens.md` lo pide para estas dos.
- `Plantilla de equipo` (`FichaJugador`) — depende de que exista `Equipo`; incremento propio.
- `Calendario`, `Resultados`, `Clasificación`, `Penalizaciones de clasificación` — dependen de que exista `Equipo`; incrementos futuros.
- Cualquier pantalla del área pública.
- Validación de que `Equipo.SedeHabitualId` pertenezca al mismo municipio que el club, o cualquier regla de negocio no exigida por `functional.md`/`data-model.md` — el desplegable permite cualquier sede.

## Criterios de aceptación

- [ ] Un administrador autenticado puede listar, dar de alta y editar `Competicion` (con desplegables de Temporada y Categoría) y `Equipo` (con desplegables de Competición, Club y Sede habitual) desde `/Admin`.
- [ ] Dar de alta o editar una `Competicion` con una combinación de temporada+categoría ya existente muestra un error de validación en español y no guarda el cambio.
- [ ] Editar un `Equipo` que otro administrador modificó mientras tanto (mismo `RowVersion` obsoleto) muestra un error de concurrencia en español y no sobrescribe el cambio ajeno.
- [ ] Existe un test de integración por entidad que cubre alta + edición + listado, y un test que verifica el rechazo de la combinación duplicada de `Competicion`.
- [ ] La rama compila y todos los tests (unitarios + integración) pasan en CI.

## Aclaraciones

- **¿Por qué agrupar Competiciones y Equipos en el mismo incremento?** → Están fuertemente acoplados: no tiene sentido probar la pantalla de Equipos sin que ya exista al menos una Competición (es una de sus claves foráneas obligatorias), y el flujo real de un administrador es "crear la competición, luego sus equipos" en la misma sesión de trabajo. Separarlos en dos incrementos añadiría ceremonia sin aportar nada, a diferencia de las cuatro pantallas de catálogo de BAS-6, que sí eran independientes entre sí.
- **¿Por qué es este el primer incremento que trata la concurrencia optimista de verdad?** → Ninguna entidad de BAS-6 (Temporada, Categoria, Club, Sede) tiene `RowVersion` en `data-model.md` — es `Equipo` la primera en tenerlo. El patrón que se establezca aquí (campo oculto + captura de `DbUpdateConcurrencyException`) es el mismo que se reutilizará en `Partido`, que también lo tiene, cuando llegue `Resultados`.

## Referencias

- [[screens]] — sección "Área admin", tabla "Gestión anual" (filas `Competiciones`, `Equipos`).
- [[data-model]] — entidades `Competicion`, `Equipo`.
- [[archive/BAS-6/spec|BAS-6]] — incremento del que depende (catálogo ya gestionable, patrón de pantallas ya establecido).
- [[archive/BAS-5/spec|BAS-5]] — restricción única de `Competicion` y `RowVersion` de `Equipo`, ambos ya en el esquema.
