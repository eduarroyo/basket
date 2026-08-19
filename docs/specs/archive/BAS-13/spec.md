---
codigo: BAS-13
titulo: Área pública — calendario y resultados (competición, partido, jornada, equipo)
estado: Completado
autor: Eduardo Arroyo
fechaCreacion: 2026-08-18
dependeDe:
  - "[[archive/BAS-12/spec|BAS-12]]"
tags:
  - backend
  - frontend
---

# BAS-13: Área pública — calendario y resultados (competición, partido, jornada, equipo)

## Descripción

BAS-12 dejó fuera de alcance el resto de pantallas públicas relacionadas con `Partido` — quedaban pendientes explícitamente para este incremento. Añade las cuatro que faltan de `screens.md`: `Calendario de competición`, `Detalle de partido`, `Resultados por jornada` y `Resultados por equipo`. Con ellas, el área pública queda completa salvo `Suscripción iCal` (depende de que exista ya un calendario público con fechas, así que va después) y `Ficha de sede` no gana todavía "próximos partidos allí" (`docs/specs/archive/BAS-12/spec.md`, Fuera de alcance — se revisa aquí, ver Aclaraciones).

## Alcance

- `Calendario de competición` (`Areas/Public/Pages/Competiciones/Calendario.cshtml`, ruta `/competiciones/{id}/calendario`): todas las jornadas de la competición con sus partidos — local, visitante, sede, fecha/hora, estado (sin marcador, es la vista de planificación). Cada jornada enlaza a su `Resultados por jornada`; cada partido enlaza a su `Detalle de partido`.
- `Resultados por jornada` (`Areas/Public/Pages/Competiciones/Jornada.cshtml`, ruta `/competiciones/{id}/jornadas/{n}`, `n` = `Jornada.Numero`): partidos de esa jornada concreta con su marcador si están jugados/resueltos, o su estado si no (aplazado/cancelado/programado). Enlaza a `Detalle de partido` de cada uno.
- `Detalle de partido` (`Areas/Public/Pages/Partidos/Index.cshtml`, ruta `/partidos/{id}`): marcador, parciales por periodo (`PartidoParcial`), sede, estado, y — si `Estado = Resuelto` — motivo y equipo ganador de la resolución administrativa. Enlaza a la ficha de ambos equipos y a la de la sede.
- `Resultados por equipo` (`Areas/Public/Pages/Equipos/Resultados.cshtml`, ruta `/equipos/{id}/resultados`): histórico completo de partidos de ese equipo (ya está scopeado a una única temporada por diseño — un `Equipo` no persiste entre temporadas, `data-model.md`). Enlaza a `Detalle de partido` de cada uno. Enlace nuevo "Resultados" desde `Ficha de equipo` (BAS-12).
- Enlace nuevo "Calendario" desde `Listado de competiciones` (BAS-12), junto al ya existente "Ver clasificación".
- Las cuatro páginas llevan Output Caching (misma política `Publico` de 4 minutos que el resto del área pública).
- Tests de integración HTTP para las cuatro páginas: contenido correcto, enlaces de navegación, 404 en `id`/`n` inexistente, Output Caching activo.

## Fuera de alcance

- `Suscripción iCal` — depende de que exista ya un calendario público con fechas (este incremento), pero es una pieza propia (librería `Ical.Net`, `architecture.md` punto 9); se hace en un incremento aparte.
- "Próximos partidos allí" en `Ficha de sede` y "próximos partidos destacados" en `Portada` — aunque ya existe toda la maquinaria de consulta de partidos tras este incremento, añadirlos ahora mezclaría este incremento (calendario/resultados) con retocar dos pantallas ya cerradas en BAS-12; se dejan para cuando haga falta esa pieza en concreto.
- Filtrar `Calendario de competición` por jornadas con `CuentaParaClasificacion = true` — ese campo solo afecta al cálculo de la clasificación (`architecture.md` punto 14), no a qué partidos existen o se muestran; el calendario muestra fase regular y fases finales por igual.
- Cualquier vista de calendario agregada por encima de una competición (p. ej. "todos los partidos de la temporada") — `screens.md` solo define el calendario a nivel de competición.

## Criterios de aceptación

- [x] `/competiciones/{id}/calendario` lista todas las jornadas de la competición y, para cada una, sus partidos con local, visitante, sede, fecha/hora y estado; cada jornada enlaza a su página de resultados y cada partido a su detalle.
- [x] `/competiciones/{id}/jornadas/{n}` muestra los partidos de esa jornada con marcador (si jugados/resueltos) o estado; un `n` inexistente en esa competición devuelve 404.
- [x] `/partidos/{id}` muestra marcador, parciales por periodo, sede, estado y — solo si `Resuelto` — motivo y ganador de la resolución; enlaza a ambos equipos y a la sede.
- [x] `/equipos/{id}/resultados` lista el histórico completo de partidos de ese equipo, cada uno enlazando a su detalle.
- [x] `Ficha de equipo` enlaza a `/equipos/{id}/resultados`; `Listado de competiciones` enlaza a `/competiciones/{id}/calendario`.
- [x] Un `id` que no existe en cualquiera de las cuatro páginas devuelve 404, no un error no controlado.
- [x] Las cuatro páginas llevan Output Caching activo (misma verificación por cabecera `Age` que BAS-11/BAS-12).
- [x] Existen tests de integración HTTP para las cuatro páginas y todos pasan en CI.

## Aclaraciones

- **¿Por qué `Calendario de competición` y `Resultados por jornada` no muestran lo mismo, si ambas listan partidos por jornada?** → `screens.md` describe columnas distintas para cada una: `Calendario` lista "fecha, hora, sede, estado" (vista de planificación, sin marcador) mientras que `Resultados por jornada` se centra en "partidos jugados/resueltos/aplazados/cancelados" (vista de resultado, con marcador cuando lo hay). `Calendario` es la vista completa de toda la temporada; `Resultados por jornada` es la página enlazable de una jornada concreta con foco en el resultado, no una duplicada.
- **¿Por qué no se añade ya "próximos partidos allí" a `Ficha de sede` si este incremento ya trae la consulta de partidos que hacía falta?** → Añadirlo aquí reabriría una pantalla que BAS-12 ya cerró y aceptó como completa, mezclando el alcance de dos incrementos. Se deja registrado como el primer candidato natural para cuando se quiera revisar esa pantalla, pero no se hace de pasada dentro de este.

## Referencias

- [[screens]] — sección "Área pública", filas `Calendario de competición`, `Detalle de partido`, `Resultados por jornada`, `Resultados por equipo`.
- [[data-model]] — entidades `Jornada`, `Partido`, `PartidoParcial`.
- [[architecture#5. Caché y rendimiento|architecture.md, punto 5]] — Output Caching, misma política que BAS-11/BAS-12.
- [[archive/BAS-12/spec|BAS-12]] — incremento del que depende (resto del núcleo navegable del área pública).
