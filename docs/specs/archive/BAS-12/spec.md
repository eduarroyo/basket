---
codigo: BAS-12
titulo: Área pública — portada, listado de competiciones y fichas de equipo/club/sede
estado: Completado
autor: Eduardo Arroyo
fechaCreacion: 2026-08-18
dependeDe:
  - "[[archive/BAS-11/spec|BAS-11]]"
tags:
  - backend
  - frontend
---

# BAS-12: Área pública — portada, listado de competiciones y fichas de equipo/club/sede

## Descripción

BAS-11 dejó lista la infraestructura de Output Caching con `Clasificación` como única pantalla pública real, alcanzable solo por URL directa (`docs/specs/archive/BAS-11/spec.md`, Fuera de alcance). Este incremento construye el resto del **núcleo navegable** del área pública: `Portada` (selector de temporada), `Listado de competiciones` y las tres fichas de entidad (`Ficha de equipo`, `Ficha de club`, `Ficha de sede`) — cerrando el camino de navegación completo Portada → temporada → competición → equipo → club/sede, sin depender de `Calendario`/`Resultados por jornada`/`Resultados por equipo` (que siguen fuera de alcance, para un incremento posterior).

## Alcance

- `Portada` (`Areas/Public/Pages/Index.cshtml`, ruta `/`, ya existe como esqueleto desde BAS-1): lista las temporadas (más reciente primero), cada una enlazando a su `Listado de competiciones`; la temporada con `Estado = EnCurso` se resalta como la activa.
- `Listado de competiciones` (`Areas/Public/Pages/Temporadas/Competiciones.cshtml`, ruta `/temporadas/{id}/competiciones`): competiciones de la temporada, agrupadas/ordenadas por `Categoria.Orden`; cada competición lista también sus equipos (enlace a su ficha) y un enlace a su `Clasificación` (BAS-11) — así deja de ser alcanzable solo por URL directa.
- `Ficha de equipo` (`Areas/Public/Pages/Equipos/Index.cshtml`, ruta `/equipos/{id}`): nombre, estado, club (enlace a su ficha), sede habitual (enlace a su ficha, si tiene), competición (temporada + categoría, con enlace al listado), y la plantilla (dorsal + posición de cada `FichaJugador`).
- `Ficha de club` (`Areas/Public/Pages/Clubes/Index.cshtml`, ruta `/clubes/{id}`): nombre, municipio, fecha de alta, y los equipos del club en la temporada con `Estado = EnCurso` (enlace a cada ficha) — tal como pide `screens.md` ("equipos del club en la temporada actual"), sin histórico de temporadas pasadas.
- `Ficha de sede` (`Areas/Public/Pages/Sedes/Index.cshtml`, ruta `/sedes/{id}`): nombre, municipio, dirección.
- Las cinco páginas llevan Output Caching (`[OutputCache(PolicyName = "Publico")]`, la política de 4 minutos ya dada de alta en BAS-11).
- Tests de integración HTTP para las cinco páginas: contenido correcto, enlaces de navegación presentes, y caché de salida activa.

## Fuera de alcance

- "Próximos partidos destacados" en `Portada` y "próximos partidos allí" en `Ficha de sede` — ambos dependen de una noción de calendario público (`Calendario de competición`, `Resultados por jornada`/`por equipo`), que sigue sin construirse; se añaden cuando llegue ese incremento.
- Histórico de un club/equipo en temporadas distintas de la actual — `Ficha de club` solo muestra la temporada `EnCurso`, tal como pide `screens.md`; no hay una vista de "todas las temporadas de este club".
- `Calendario de competición`, `Detalle de partido`, `Resultados por jornada`, `Resultados por equipo`, `Suscripción iCal` — resto de pantallas públicas de `screens.md`, incrementos futuros.
- Cualquier interacción JavaScript (selector dinámico, autocompletado) — la navegación es enlaces simples, coherente con la decisión de Razor Pages sin SPA (`architecture.md` punto 1).

## Criterios de aceptación

- [x] `/` lista las temporadas existentes, la más reciente primero, cada una enlazando a `/temporadas/{id}/competiciones`; la temporada `EnCurso` (si existe) se distingue visualmente de las demás.
- [x] `/temporadas/{id}/competiciones` lista las competiciones de esa temporada agrupadas por categoría, cada una con sus equipos enlazados a su ficha y un enlace a su clasificación.
- [x] `/equipos/{id}` muestra el equipo, su club y sede habitual (ambos enlazados a su ficha si existen) y su plantilla completa.
- [x] `/clubes/{id}` muestra el club y sus equipos de la temporada `EnCurso`, enlazados a su ficha; si no hay ninguna temporada `EnCurso`, lo indica sin fallar.
- [x] `/sedes/{id}` muestra nombre, municipio y dirección de la sede.
- [x] Las cinco páginas llevan Output Caching activo (misma verificación por cabecera `Age` que BAS-11).
- [x] Un `id` que no existe en cualquiera de las cinco páginas devuelve 404, no un error no controlado.
- [x] Existen tests de integración HTTP para las cinco páginas y todos pasan en CI.

## Aclaraciones

- **¿Por qué `Portada` no muestra ya "próximos partidos destacados", si `screens.md` lo pide?** → Esa pieza necesita decidir qué partidos son "destacados" (¿los más próximos en el tiempo? ¿uno por competición?) y consultar `Partido.FechaHora`, algo que tiene más sentido resolver junto con `Calendario de competición`/`Resultados por jornada` (mismo tipo de consulta, mismo incremento futuro) que aislado aquí. Sin esa pieza, `Portada` sigue cumpliendo su función principal: seleccionar temporada y navegar.
- **¿Por qué no hay un "listado de equipos" ni un "listado de clubes/sedes" independientes?** → `screens.md` no los define como pantallas propias — la navegación prevista es `Listado de competiciones` → equipos de cada competición → `Ficha de equipo` → club/sede habitual de ese equipo. `Ficha de club` añade una segunda entrada (equipos del club en la temporada actual), también ya contemplada en `screens.md`. Ninguna ficha necesita un listado dedicado de su propio tipo para ser alcanzable.
- **¿Cómo se decide qué equipos aparecen en `Ficha de club` si no hay ninguna temporada `EnCurso`?** → Se muestra la ficha del club igualmente (nombre, municipio, fecha de alta) con la sección de equipos vacía y un texto que lo indica, en vez de un error — situación normal entre temporadas (por ejemplo, verano, cuando la anterior ya está `Finalizada`/`Archivada` y la siguiente sigue `Planificada`).

## Referencias

- [[screens]] — sección "Área pública", filas `Portada`, `Listado de competiciones`, `Ficha de equipo`, `Ficha de club`, `Ficha de sede`.
- [[data-model]] — entidades `Temporada`, `Competicion`, `Categoria`, `Equipo`, `Club`, `Sede`, `FichaJugador`.
- [[architecture#5. Caché y rendimiento|architecture.md, punto 5]] — Output Caching, misma política que BAS-11.
- [[archive/BAS-11/spec|BAS-11]] — incremento del que depende (Clasificación y la infraestructura de Output Caching ya dadas de alta).
