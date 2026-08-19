---
codigo: BAS-11
titulo: Clasificación (consulta calculada) — vista admin y pública
estado: Completado
autor: Eduardo Arroyo
fechaCreacion: 2026-08-18
dependeDe:
  - "[[archive/BAS-10/spec|BAS-10]]"
tags:
  - backend
  - frontend
---

# BAS-11: Clasificación (consulta calculada) — vista admin y pública

## Descripción

Con `Partido` ya planificable (BAS-9) y con resultado (BAS-10), este incremento añade la `Clasificación` de una competición: la tabla ordenada de equipos que `architecture.md` (punto 14) y `data-model.md` definen como una **consulta calculada, sin tabla propia** — se agregan los `Partido` en estado `Jugado`/`Resuelto` de las jornadas que cuentan para clasificación (`Jornada.CuentaParaClasificacion`), más las `PenalizacionClasificacion` vigentes de cada equipo (ya modeladas desde BAS-5, aunque su pantalla de alta llega en un incremento posterior — la resta ya funciona sobre cero penalizaciones).

Es la primera pantalla pública real del proyecto (hasta ahora solo existía el esqueleto `Areas/Public/Pages/Index.cshtml`), así que este incremento también da de alta la infraestructura de Output Caching (`architecture.md` punto 5) que el resto de pantallas públicas futuras reutilizará.

## Alcance

- Servicio compartido `ClasificacionService` (`src/BasketBaseTracker.Web/Domain/ClasificacionService.cs`): consulta los `Partido`/`Equipo`/`PenalizacionClasificacion` de una competición y produce la tabla ordenada. Es el mismo servicio que consumen la vista admin y la pública (`screens.md`: "misma consulta que la pública, sin caché").
- Lógica de agregación pura y testeada como unitaria, `src/BasketBaseTracker.Web/Domain/ClasificacionCalculator.cs` (`architecture.md` punto 15): a partir de una lista de partidos (local/visitante/marcador) y de penalizaciones, calcula por equipo partidos jugados, victorias, derrotas, puntos a favor/en contra, puntos de clasificación y diferencia de tantos.
- **Desempate: solo el que no depende de en qué fase está la competición** — puntos de clasificación, y si hay empate, diferencia general de tantos y luego cociente general de tantos (`reglamento/resumen-reglas-relevantes.md`, §3, primeros dos criterios de la lista "hasta el final de la primera vuelta"). Ver Aclaraciones para el resto de criterios, deliberadamente fuera de alcance.
- Página pública `Areas/Public/Pages/Competiciones/Clasificacion.cshtml`, ruta `/competiciones/{id}/clasificacion` (`screens.md`), con Output Caching (`architecture.md` punto 5: TTL 4 minutos).
- Página admin `Areas/Admin/Pages/Clasificacion/Index.cshtml`, ruta `/Admin/Clasificacion/{competicionId}`, sin caché — enlace "Clasificación" añadido a `Competicion/Index` (BAS-7), junto al ya existente "Calendario" (BAS-9).
- Alta de la infraestructura de Output Caching en `Program.cs` (`AddOutputCache`/`UseOutputCache`, una política con expiración de 4 minutos) — primer consumidor de esta infraestructura; queda lista para que futuras pantallas públicas la reutilicen sin volver a configurarla.
- Tests: unitarios para `ClasificacionCalculator` (orden por puntos, desempate por diferencia y cociente, penalización resta puntos, equipo sin partidos aparece con todo a cero) y de integración HTTP para ambas páginas (admin y pública), incluyendo que las jornadas con `CuentaParaClasificacion = false` no cuentan.

## Fuera de alcance

- Criterios de desempate que dependen de la fase de la competición (enfrentamiento directo desde la segunda vuelta, detección de "primera vuelta", regla del equipo con sanción 2-0 en última posición) — `architecture.md` ya señala que decidir automáticamente en qué fase está una competición no está resuelto; se aplaza a un incremento futuro cuando haga falta esa precisión.
- Pantalla de alta de `PenalizacionClasificacion` — sigue siendo, como ya anticipaba `screens.md`, una de las últimas piezas previstas. Este incremento solo consume la tabla (hoy vacía) en el cálculo.
- Cualquier pantalla pública que enlace hacia esta (`Portada`, `Listado de competiciones`) — no existen todavía; se accede por URL directa mientras tanto.
- Purga activa de caché al guardar un partido — la decisión ya tomada en `architecture.md` punto 5 es TTL puro, sin invalidación activa.
- Retirada de un equipo de la competición completa (Art. 50 del Reglamento Disciplinario) — no se modela; si ocurre, el administrador gestiona los partidos pendientes a mano y el equipo queda en la tabla con su registro parcial.

## Criterios de aceptación

- [x] La página pública `/competiciones/{id}/clasificacion` muestra la tabla de la competición ordenada por puntos, con el resto de columnas (PJ, V, D, PF, PC, diferencia).
- [x] La página admin `/Admin/Clasificacion/{competicionId}` muestra exactamente la misma tabla, accesible solo para administradores autenticados, sin caché.
- [x] Un equipo sin partidos jugados aparece en la tabla con todos los valores a cero, no desaparece.
- [x] Los partidos de una jornada con `CuentaParaClasificacion = false` no se cuentan en la tabla.
- [x] Un empate a puntos se resuelve por diferencia general de tantos y, si persiste, por cociente general de tantos.
- [x] Una `PenalizacionClasificacion` de un equipo resta de sus puntos de clasificación.
- [x] La respuesta pública lleva cabecera de caché de salida (Output Caching) con expiración de 4 minutos; la respuesta admin no.
- [x] Existen tests unitarios para `ClasificacionCalculator` y tests de integración HTTP para ambas páginas, y todos pasan en CI.

## Aclaraciones

- **¿Por qué solo dos de los cinco criterios de desempate del Art. 84-85?** → Los otros tres (puntos/diferencia/tantos entre los propios equipos empatados, es decir enfrentamiento directo) solo aplican "desde el inicio de la segunda vuelta y en Campeonatos de Andalucía" según el propio reglamento — y `architecture.md` ya deja anotado que determinar automáticamente en qué fase está una competición es una decisión de modelo de datos sin resolver (podría inferirse de `Jornada.Numero` frente al total de jornadas de liga regular, o marcarse explícitamente). Implementarlo ahora exigiría resolver esa decisión de paso, agrandando este incremento más allá de lo que pide la clasificación básica. Los dos criterios que sí se implementan (diferencia y cociente general de tantos) son válidos en cualquier fase, así que cubren el caso común sin esa dependencia.
- **¿Por qué no se aplica ya la regla del equipo con sanción 2-0 en última posición?** → Esa regla (Art. 84.iv/85.c) depende de qué partidos fueron resueltos administrativamente con marcador 2-0 en contra de un equipo por una sanción concreta, distinguiéndolos de un 2-0 real de juego — información que hoy no se puede derivar solo de `Partido.PuntosLocal`/`PuntosVisitante` (dos marcadores 2-0 se ven idénticos en la base de datos, sea por sanción o, en teoría, por un final ajustadísimo). Se deja para cuando exista `PenalizacionClasificacion` en la UI, que si es el vehículo natural para registrar ese tipo de sanción.
- **¿Por qué el servicio de clasificación no es "puro" pero sí lo es `ClasificacionCalculator`?** → `ClasificacionService` hace las consultas EF Core (I/O); `ClasificacionCalculator` recibe listas ya cargadas en memoria y solo agrega/ordena, sin tocar la base de datos — así se puede testear como unitario sin arrancar el `AppHost` completo, tal como pide `architecture.md` punto 15 explícitamente para "cálculo de la clasificación".

## Referencias

- [[screens]] — sección "Área pública" y "Área admin", filas `Clasificación`.
- [[data-model#Clasificación de una competición (calculada, sin tabla propia)|data-model.md]] — campos que produce el cálculo.
- [[architecture#14. Clasificación como consulta calculada|architecture.md, punto 14]] — decisión de consulta calculada y criterios de desempate.
- [[architecture#5. Caché y rendimiento|architecture.md, punto 5]] — Output Caching, TTL de 4 minutos.
- [[reglamento/resumen-reglas-relevantes#3-desempates-en-la-clasificación-fab-art-8485|resumen-reglas-relevantes.md, §3]] — criterios de desempate completos.
- [[archive/BAS-10/spec|BAS-10]] — incremento del que depende (`Partido` ya con resultado).
