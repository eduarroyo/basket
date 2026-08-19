---
codigo: BAS-17
titulo: Datos de demostración (seed multi-temporada)
estado: Completado
autor: Eduardo Arroyo
fechaCreacion: 2026-08-19
tags:
  - backend
  - herramientas-desarrollo
---

# BAS-17: Datos de demostración (seed multi-temporada)

## Descripción

BasketBaseTracker necesita poder desplegarse en cualquier entorno (local o de demostración) con un conjunto de datos realista que permita enseñar la funcionalidad completa de la aplicación: varias temporadas de histórico, varios clubes con sus equipos, jugadores y sedes, partidos con parciales y resultados, y una temporada actual **a medio disputar** (con jornadas ya jugadas y otras todavía programadas).

Se descartan dos alternativas evaluadas antes de escribir esta spec:

- **Script SQL que borra la base de datos y hace `INSERT`s directos**: se desincroniza con el esquema en cuanto haya una migración nueva, y no respeta las invariantes de negocio del modelo que no son expresables como constraint de base de datos — la más relevante aquí, que un equipo no puede aparecer dos veces en la misma jornada (`data-model.md`).
- **Fichero de datos (JSON) cargado tal cual vía EF Core, anticipando el futuro formato de import/export** (`architecture.md`, punto 10): tiene el mismo problema que el SQL — ni JSON ni EF Core validan por sí solos esa invariante — y además ese incremento de import/export todavía no existe (no hay spec `BAS-N` para él), así que esperarlo bloquearía innecesariamente este trabajo, que tiene un propósito distinto (datos de demostración, no backup/migración).

La alternativa elegida es un **comando de seeding explícito, separado del arranque normal de la aplicación**, que genera los datos en C#: las entidades catálogo (categorías, clubes, sedes, jugadores) se declaran/generan directamente, y el calendario de cada competición (jornadas y partidos) se genera mediante un **algoritmo de emparejamiento round-robin** sobre los equipos de esa competición — un procedimiento que garantiza *por construcción* que ningún equipo se repite en la misma jornada, en vez de depender de escribir a mano (o validar a posteriori) cada partido.

## Alcance

- Un comando/target `dotnet` explícito y separado del arranque normal de `BasketBaseTracker.Web` (no ligado a `Program.cs` de la app en ningún entorno), que puebla la base de datos configurada con un dataset de demostración completo.
- Genera, en este orden de dependencia: `Categoria`, `Sede`, `Club`, varias `Temporada` (mezcla de estados `Finalizada`/`Archivada` y una `EnCurso`), `Competicion` (una por cada combinación razonable de `Temporada` x `Categoria`, dentro de los rangos de `functional.md`), `Equipo` (participación de cada club en cada competición), `FichaJugador` por equipo, `Jornada` y `Partido` (generados por el algoritmo round-robin), y `PartidoParcial` para los partidos ya jugados.
- La temporada `EnCurso` tiene una mezcla de partidos en estado `Jugado` (con resultado y parciales coherentes) y `Programado` (con fecha futura, sin resultado) — el resto de temporadas (`Finalizada`/`Archivada`) tiene todos sus partidos `Jugado`.
- Escala del dataset dentro de los rangos ya estimados en `functional.md` (8-14 equipos por categoría y competición, 8-20 jugadores por equipo).
- Salvaguarda para no ejecutarlo por error contra producción (mecanismo concreto a definir en `plan.md` — p. ej. requerir una confirmación explícita o bloquear por variable de entorno/cadena de conexión).

## Fuera de alcance

- El incremento de import/export completo (`architecture.md`, punto 10) — no existe todavía y no es una dependencia de este trabajo.
- Ejecución automática del seeding al arrancar la aplicación, en cualquier entorno — siempre es una acción manual y explícita, a diferencia de `IdentitySeeder` (que sí corre en cada arranque porque es idempotente y de bajo riesgo).
- `PenalizacionClasificacion` — no forma parte del dataset inicial; su implementación es de las últimas piezas previstas según `data-model.md` y se puede añadir al seeder más adelante sin rediseñarlo.
- Estados `Aplazado`/`Cancelado`/`Resuelto` de `Partido` — el dataset inicial cubre `Programado` y `Jugado`; ampliar el generador a los estados administrativos especiales queda para un incremento posterior si hace falta demostrar también esa casuística.
- Cualquier cambio a las pantallas de `screens.md` — este incremento solo alimenta datos, no añade ni modifica pantallas.

## Criterios de aceptación

- [x] Ejecutar el comando de seeding contra una base de datos vacía puebla `Categoria`, `Sede`, `Club`, varias `Temporada`, `Competicion`, `Equipo`, `FichaJugador`, `Jornada` y `Partido`.
- [x] Existe al menos una `Temporada` en estado `Finalizada` o `Archivada` con todos sus partidos en estado `Jugado` y resultado (incluidos `PartidoParcial`).
- [x] Existe una `Temporada` en estado `EnCurso` con una mezcla de partidos `Jugado` (con resultado y parciales) y `Programado` (con fecha futura, sin resultado).
- [x] En ninguna `Jornada` generada aparece el mismo `Equipo` dos veces (ni como local ni como visitante).
- [x] El comando de seeding no se ejecuta como parte del arranque normal de `BasketBaseTracker.Web` — solo se dispara explícitamente.
- [x] El comando incluye una salvaguarda explícita que impide (o exige confirmación reforzada) ejecutarlo contra la base de datos de producción.
- [x] Existe un workflow de GitHub Actions (`workflow_dispatch`, con inputs para nº de temporadas, nº de clubes y si se borran los datos existentes) que ejecuta el seeding contra Azure SQL, reutilizando el patrón de firewall temporal de `deploy.yml` — sin ningún recurso de Azure nuevo (nada de Azure Functions).
- [x] Tras ejecutar el seeding, las pantallas públicas de calendario, resultados y clasificación (`screens.md`) muestran datos coherentes para al menos una competición de la temporada `EnCurso`.

## Aclaraciones

- **¿Script SQL, fichero de datos (JSON) o generación en C#?** → C#, con generación *procedural* del calendario (no solo entidades catálogo escritas a mano). Se descartó el SQL porque se desincroniza con el esquema y no respeta invariantes de aplicación. Se descartó también un fichero JSON cargado tal cual por el mismo motivo — ni JSON ni C# "a pelo" (construyendo entidades una a una y llamando a `SaveChanges`) validan solos la invariante de "equipo no repetido en la jornada"; lo que sí la garantiza es el *proceso*: un algoritmo de emparejamiento round-robin que combina los equipos de una misma competición produce, por construcción, un calendario donde cada equipo aparece como mucho una vez por jornada — sin depender de validación a posteriori. C# además da seguridad de tipos en compilación (typos de enum, campos renombrados en un refactor rompen el build del seeder en vez de fallar en silencio), ventaja que un fichero de datos externo no ofrece.
- **¿Por qué no esperar al import/export de `architecture.md` punto 10?** → No está implementado (no existe spec `BAS-N` para él) y su propósito declarado es backup/migración en producción, no generación de datos de demostración — son necesidades distintas; bloquear este trabajo en esa pieza retrasaría sin necesidad el objetivo real (tener datos de demo ya).
- **¿Cómo se invoca el seeding?** → Comando explícito y separado del arranque normal de la aplicación (no un flag de configuración evaluado en cada arranque, a diferencia de `IdentitySeeder`), para eliminar el riesgo de que se dispare sin querer en un entorno con datos reales. El mecanismo concreto (herramienta CLI dedicada, target de `dotnet run`, etc.) se decide en `plan.md`.
- **¿Ejecutarlo desde una Azure Function aislada, lanzada manualmente desde Azure?** → Descartado. Añadiría un recurso de cómputo nuevo (Function App + storage account + hosting plan) solo para una operación que se dispara rara vez y siempre a mano — el mismo motivo por el que `architecture.md` (punto 4) ya evita descomponer la aplicación en Azure Functions. En su lugar, se reutiliza el patrón que el proyecto ya tiene para ejecutar una operación puntual contra Azure SQL desde fuera de Azure: el paso "Aplicar migraciones de base de datos" de `deploy.yml` (`architecture.md`, punto 13), que abre una regla de firewall temporal en Azure SQL para la IP del runner, ejecuta el comando (`dotnet ef database update`) y la cierra al terminar. El seeding se dispara como un workflow de GitHub Actions independiente con `workflow_dispatch` (no ligado a ningún push/PR), con inputs para los parámetros (nº de temporadas, nº de clubes, si se borran los datos existentes) — mismo mecanismo de disparo manual que ya usa `deploy.yml`, sin infraestructura nueva que mantener ni superficie HTTP adicional que asegurar.
- **Nota importante de alcance**: `architecture.md` (punto 13) fija un único entorno en la nube — producción, sin `staging` (`MF-4`, descartado explícitamente). Por tanto, ejecutar este workflow contra Azure SQL significa ejecutarlo contra la base de datos de producción real; no existe un entorno de demo separado en Azure. Es una operación **destructiva** (puede borrar datos existentes) que debe reservarse a antes de que existan datos de competición reales, o usarse deliberadamente para mostrar la aplicación en vivo — nunca disparo accidental. Refuerza la necesidad de una confirmación explícita en el propio workflow (más allá de que `workflow_dispatch` ya exige disparo manual), a definir en `plan.md`.

## Referencias

- [[data-model]] — todas las entidades catálogo, ancladas a temporada y de calendario/resultados que puebla el seed.
- [[screens]] — pantallas públicas de consulta (calendario, resultados, clasificación, fichas de equipo/club/sede) que este incremento alimenta con datos, sin modificarlas.
- [[architecture#10. Importación/exportación|architecture.md, punto 10]] — por qué no se depende del futuro import/export.
- [[architecture#15. Estrategia de pruebas|architecture.md, punto 15]] — contexto de cómo se levanta la base de datos real en tests de integración, referencia útil para decidir en `plan.md` cómo se ejecuta el comando de seeding en desarrollo local.
