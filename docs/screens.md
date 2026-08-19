# Inventario de pantallas

Este fichero describe el inventario de pantallas de BasketBaseTracker, derivado de los requisitos descritos en [`functional.md`](./functional.md) y de las entidades definidas en [`data-model.md`](./data-model.md).

Las pantallas se organizan en dos áreas (ver decisión de arquitectura: un único proyecto ASP.NET Core con Areas `Public`/`Admin`, Razor Pages en ambas).

## Área pública (usuarios anónimos)

Pantallas de solo lectura, candidatas directas a Output Caching.

| Pantalla | Descripción | Entidades principales | Ruta sugerida |
| --- | --- | --- | --- |
| Portada | Selector de temporada activa, accesos a categorías, próximos partidos destacados | Temporada, Competicion | `/` |
| Listado de competiciones | Competiciones de la temporada seleccionada, una por categoría | Competicion, Categoria | `/temporadas/{id}/competiciones` |
| Calendario de competición | Partidos agrupados por jornada (fecha, hora, sede, estado) | Jornada, Partido | `/competiciones/{id}/calendario` |
| Detalle de partido | Marcador, parciales por cuarto, sede, estado, motivo si es resuelto administrativamente | Partido, PartidoParcial | `/partidos/{id}` |
| Resultados por jornada | Partidos jugados/resueltos/aplazados/cancelados de una jornada concreta | Jornada, Partido | `/competiciones/{id}/jornadas/{n}` |
| Resultados por equipo | Histórico de partidos de un equipo en la temporada | Equipo, Partido | `/equipos/{id}/resultados` |
| Clasificación | Tabla ordenada de la competición | Partido (calculada) | `/competiciones/{id}/clasificacion` |
| Ficha de equipo | Plantilla (dorsales/posiciones), club, sede habitual | Equipo, FichaJugador | `/equipos/{id}` |
| Ficha de club | Equipos del club en la temporada actual | Club, Equipo | `/clubes/{id}` |
| Ficha de sede | Dirección, municipio, próximos partidos allí | Sede, Partido | `/sedes/{id}` |
| Suscripción iCal | Enlace `.ics` descargable/suscribible (por competición y por equipo) | Partido | `/competiciones/{id}/calendario.ics`, `/equipos/{id}/calendario.ics` |

## Área admin (autenticado)

### Catálogo

| Pantalla | Descripción | Entidad |
| --- | --- | --- |
| Temporadas | Listado / alta / edición / cambio de estado | Temporada |
| Categorías | Listado / alta / edición | Categoria |
| Clubes | Listado / alta / edición | Club |
| Sedes | Listado / alta / edición | Sede |

### Gestión anual

| Pantalla | Descripción | Entidad |
| --- | --- | --- |
| Competiciones | Alta/edición por temporada+categoría, configurar puntos victoria/derrota | Competicion |
| Equipos | Alta/edición, asignar club, competición y sede habitual | Equipo |
| Plantilla de equipo | Alta/baja/edición de fichas (dorsal, posición) dentro de un equipo | FichaJugador |
| Calendario (planificación) | Crear jornadas, programar partidos (equipos, fecha/hora, sede) | Jornada, Partido |
| Resultados | Introducir marcador y parciales, o cambiar estado (aplazado/cancelado/resuelto + motivo + ganador) | Partido, PartidoParcial |
| Clasificación | Vista de verificación del cálculo (misma consulta que la pública, sin caché) | Partido (calculada) |
| Penalizaciones de clasificación | Alta de una penalización de puntos a un equipo, con motivo y partido vinculado opcional | PenalizacionClasificacion |

`Calendario` y `Resultados` se mantienen como dos pantallas separadas aunque compartan la entidad `Partido`, porque el documento funcional las trata como dos funciones distintas (planificación vs. resultado).

`Penalizaciones de clasificación` es de las últimas pantallas previstas para implementarse — ver [[architecture#14. Clasificación como consulta calculada|architecture.md, punto 14]], nota de secuenciación.

### Sistema

| Pantalla | Descripción | Estado |
| --- | --- | --- |
| Login | Acceso de administradores | v1 |
| Importación/exportación | Exportación e importación **completa** de todos los datos del sistema desde un único lugar (no parcial por entidad). Su propósito es servir de backup y de vía de migración a otra plataforma si fuera necesario, no la gestión del día a día. Solo accesible al rol `Administrador` (sistema) — ver [[archive/BAS-16/spec\|BAS-16]]. | v1 |
| Usuarios/roles | Alta de administradores; preparado para desglosar en roles más específicos en el futuro (gestor de equipos, de resultados...) — BAS-16 ya dividió el rol único original en `GestorCompeticion`/`Administrador`, esta pantalla sigue sin interfaz propia (alta manual) | Placeholder |

## Resumen

- 11 pantallas públicas
- 14 pantallas admin (12 de v1 + login, y 1 placeholder: usuarios/roles)
- Total: ~25 pantallas, todas mapeadas 1:1 al modelo de datos para facilitar el scaffolding de Razor Pages.
