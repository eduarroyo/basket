---
codigo: BAS-14
titulo: Suscripción iCal (calendario por competición y por equipo)
estado: Completado
autor: Eduardo Arroyo
fechaCreacion: 2026-08-19
dependeDe:
  - "[[archive/BAS-13/spec|BAS-13]]"
tags:
  - backend
  - frontend
---

# BAS-14: Suscripción iCal (calendario por competición y por equipo)

## Descripción

`BAS-13` cerró el grafo de navegación del área pública (calendario, resultados, clasificación) dejando explícitamente fuera la suscripción iCal. Este incremento la añade: dos feeds `.ics` (por competición y por equipo) que un cliente de calendario (Google Calendar, Apple Calendar, Outlook...) puede añadir por URL y que se mantienen actualizados sin intervención manual, cumpliendo el requisito de `functional.md` ("Calendario ical/google calendar"). La decisión de librería (`Ical.Net`) y el alcance de las dos rutas ya están fijados en `architecture.md` (punto 9) y `screens.md`.

## Alcance

- Endpoint `/competiciones/{id}/calendario.ics`: feed iCal con los partidos de una competición.
- Endpoint `/equipos/{id}/calendario.ics`: feed iCal con los partidos de un equipo (local o visitante).
- Generación del feed con `Ical.Net`, un `VEVENT` por partido con fecha/hora, equipos, sede y estado.
- Enlace de suscripción ("Añadir a mi calendario" o equivalente) visible en las páginas públicas ya existentes que muestran ese calendario: Calendario de competición y Ficha de equipo.
- Cabeceras HTTP correctas para un feed iCal (`Content-Type: text/calendar; charset=utf-8`) para que los clientes de calendario lo reconozcan al suscribirse por URL.
- Reutiliza el Output Caching del área pública (`architecture.md`, punto 5) para el TTL del feed.

## Fuera de alcance

- Integración con la API de Google Calendar (push de eventos a una cuenta) — el propio requisito funcional se satisface con un feed `.ics` suscribible, sin necesidad de esa integración.
- Notificaciones/recordatorios push a los usuarios — eso lo gestiona el propio cliente de calendario del usuario, no esta aplicación.
- Cualquier cambio a `Partido`, `Jornada` o `Equipo` en `data-model.md` — este incremento solo lee datos ya existentes.
- Purga activa de caché al modificar un partido — mismo TTL por expiración que el resto del área pública, sin excepción para el feed.

## Criterios de aceptación

- [x] `GET /competiciones/{id}/calendario.ics` devuelve un fichero iCal válido (`Content-Type: text/calendar`) con un `VEVENT` por cada partido de esa competición.
- [x] `GET /equipos/{id}/calendario.ics` devuelve un fichero iCal válido con un `VEVENT` por cada partido (local o visitante) de ese equipo.
- [x] Cada `VEVENT` incluye al menos: resumen (equipos enfrentados), fecha/hora de inicio, ubicación (sede) si existe, y un UID estable por partido.
- [x] Un ID de competición o equipo inexistente devuelve 404.
- [x] El feed generado importa correctamente en al menos un cliente real (Google Calendar, validado manualmente).
- [x] Las páginas públicas de Calendario de competición y Ficha de equipo muestran un enlace visible a su feed `.ics` correspondiente.
- [x] El feed se sirve con el mismo TTL de caché que el resto de páginas públicas (`architecture.md`, punto 5), sin necesidad de purga activa.

## Aclaraciones

- **Estados incluidos en el feed**: se incluyen como `VEVENT` normal todos los partidos con `FechaHora` no nula, cualquiera que sea su `Estado` (Programado, Jugado, Aplazado con nueva fecha, Resuelto), **excepto** los partidos en `Estado = Cancelado`, que se excluyen del feed aunque tuvieran `FechaHora` asignada antes de cancelarse. Un partido `Aplazado` sin nueva fecha (`FechaHora` nula) tampoco aparece hasta que se reprograme.
- **Duración del evento**: el modelo de datos no registra hora de fin, solo `FechaHora` de inicio (`data-model.md#Partido`). Cada `VEVENT` usa una duración fija de 2 horas para todas las categorías, sin dato adicional.
- **Alcance temporal**: el feed incluye todos los partidos de la competición/equipo en la temporada (pasados y futuros), sin filtrar por fecha actual — más simple de implementar y permite ver el histórico en el propio calendario suscrito.
- **Validación manual con Google Calendar**: se importó un feed real (`GET /competiciones/{id}/calendario.ics` contra la app en local) mediante "Importar" (Ajustes → Importar y exportar) y el evento se mostró correctamente (equipos, fecha, duración de 2h). Esa vía es una foto fija de un solo uso — no es la mecánica de suscripción prevista. La mecánica real ("Añadir calendario → Desde URL") es la que ya soporta el endpoint tal cual está: Google memoriza la URL y la vuelve a consultar periódicamente (cadencia decidida por Google, no configurable por nosotros) sin que el usuario tenga que volver a importar nada. No se pudo probar esa vía en esta sesión porque Google no puede alcanzar `https://localhost` para volver a consultarlo; queda pendiente de una verificación natural en el primer despliegue con URL pública, no de ningún cambio de código — la decisión de arquitectura (punto 9) y el "Fuera de alcance" de este documento (sin integración con la API de Google Calendar) ya asumían precisamente esta mecánica de suscripción por URL.

## Referencias

- [[data-model#Partido|data-model.md — Partido]], [[data-model#Jornada|Jornada]], [[data-model#Equipo|Equipo]], [[data-model#Sede|Sede]] — entidades leídas para construir el feed.
- [[screens]] — pantallas `Calendario de competición`, `Ficha de equipo` (enlace de suscripción) y `Suscripción iCal` (rutas `.ics`).
- [[architecture#9. Calendario iCal|architecture.md, punto 9]] — decisión de librería (`Ical.Net`).
- [[architecture#5. Caché y rendimiento|architecture.md, punto 5]] — política de caché reutilizada por el feed.
- [[archive/BAS-13/spec|BAS-13]] — incremento del que depende (calendario/resultados públicos ya existentes, sobre los que se construye el feed).
