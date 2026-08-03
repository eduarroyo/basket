---
codigo: BAS-1
titulo: Documentación funcional, de arquitectura, modelo de datos y pantallas
estado: Archivado
autor: Eduardo Arroyo
fechaCreacion: 2026-08-01
tags:
  - documentación
---

# BAS-1: Documentación funcional, de arquitectura, modelo de datos y pantallas

## Descripción

Sentar la base documental completa del proyecto antes de escribir ninguna línea de código: qué se construye y por qué (`functional.md`), cómo se organiza el dato (`data-model.md`), qué pantallas expone (`screens.md`), qué decisiones de arquitectura lo sostienen y por qué (`architecture.md`), y cómo se trabaja en los incrementos sucesivos (`workflow.md`).

## Alcance

- `functional.md` — funcionalidades, requisitos no funcionales, recursos de desarrollo, usuarios del sistema.
- `data-model.md` — entidades, relaciones, invariantes de diseño (aislamiento por temporada/categoría, RGPD, clasificación como cálculo derivado).
- `screens.md` — inventario de pantallas públicas y de administración, mapeadas 1:1 al modelo de datos.
- `architecture.md` — 17 decisiones de arquitectura numeradas (backend/frontend, persistencia, hosting, caché, seguridad, autenticación, observabilidad, iCal, import/export, IaC, registro de contenedores, CI/CD, clasificación calculada, estrategia de pruebas, formato de competición, retención de datos) más una sección `Mejoras futuras` con las alternativas descartadas y por qué.
- `workflow.md` — el propio ciclo de spec-driven development que rige este documento.
- Consulta y resumen de la normativa real de baloncesto base aplicable (Reglas Oficiales FIBA 2022, Reglamento General y de Competiciones de la F.A.B., Reglamento Disciplinario de la F.A.B.) en `docs/reglamento/resumen-reglas-relevantes.md`, usada para fundamentar decisiones concretas del modelo de datos (sistema de puntos, desempates, marcador técnico, penalizaciones de clasificación).

## Fuera de alcance

- Cualquier código fuente (`src/`, `tests/`, `infra/`) — corresponde a `BAS-2` en adelante.
- Resolver el Reglamento de Régimen Disciplinario más allá de lo ya localizado (no quedan puntos abiertos conocidos a fecha de cierre).

## Criterios de aceptación

- [x] `functional.md`, `data-model.md`, `screens.md`, `architecture.md` y `workflow.md` existen y son consistentes entre sí (referencias cruzadas verificadas).
- [x] `architecture.md` no tiene decisiones pendientes de detalle operativo relevantes para empezar a implementar (estrategia de pruebas, observabilidad, caché/invalidación, cálculo de clasificación y desempates, seguridad operativa, import/export, formato de competición, retención de datos — todas resueltas y numeradas).
- [x] `data-model.md` no tiene entradas en "Pendiente de definir".
- [x] Las reglas de baloncesto base realmente aplicables (puntuación, desempates, marcador técnico, penalizaciones) están verificadas contra las fuentes oficiales (FIBA, FAB), no inventadas.

## Aclaraciones

Esta spec se redacta **a posteriori**, una vez completado y ya fusionado a `develop` todo el trabajo que describe — no existía como carpeta `docs/specs/BAS-1/` mientras se hacía el trabajo, porque la disciplina de spec-driven development de `workflow.md` se adoptó y se fue refinando durante el propio desarrollo de esta documentación. Se crea ahora, directamente en `docs/specs/archive/`, para que quede constancia en el histórico de specs y `BAS-2` sea coherente con la numeración secuencial.

Nota de proceso a tener en cuenta para incrementos futuros: el refinamiento iterativo de esta documentación (estrategia de pruebas, observabilidad, caché, clasificación, seguridad, import/export, formato de competición, reglamento FIBA/FAB) se hizo mediante commits directos a `develop`, no en una rama `feature/BAS-1`, porque el incremento ya se había fusionado antes de empezar ese refinamiento. A partir de `BAS-2`, todo el trabajo — incluidas las revisiones de documentación de un incremento ya creado — debe hacerse en su rama `feature/BAS-N` correspondiente, tal como fija `workflow.md`.

## Referencias

- [[data-model]] — modelo de datos completo.
- [[screens]] — inventario de pantallas.
- [[architecture]] — decisiones de arquitectura y mejoras futuras.
