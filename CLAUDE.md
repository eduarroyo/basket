# BasketBaseTracker

Plataforma de seguimiento de la competición de baloncesto base de la provincia de Sevilla. Ver `README.md` para la descripción completa.

## Documentación de referencia (leer antes de trabajar)

- `docs/functional.md` — requisitos funcionales y no funcionales.
- `docs/data-model.md` — modelo de datos.
- `docs/screens.md` — inventario de pantallas.
- `docs/architecture.md` — decisiones de arquitectura.
- `docs/workflow.md` — flujo de trabajo de desarrollo (spec-driven development).

## Reglas de trabajo obligatorias

- Todo el trabajo de desarrollo sigue el ciclo de `docs/workflow.md`: cada incremento es una spec `BAS-N` en `docs/specs/BAS-N/` (spec.md → plan.md → tasks.md → implementación → cierre).
- Cada incremento se trabaja en su propia rama `feature/BAS-N`, creada desde `develop`.
- **Nunca fusionar un PR ni borrar una rama sin confirmación humana explícita**, aunque todas las tareas de `tasks.md` estén completas.
- `main` y `develop` son ramas protegidas en GitHub mediante Rulesets (configuración gestionada directamente por el propietario del repositorio, no por el asistente): sin *push* directo, PR obligatorio y la pipeline de CI (build + tests) en verde antes de poder fusionar — refuerzo a nivel de plataforma de la regla anterior. La exigencia habitual de "al menos una aprobación de un desarrollador" queda **suspendida deliberadamente**: al haber un único desarrollador, no hay nadie distinto del propio contribuidor que pueda aprobar el PR (`required_approving_review_count: 0` en ambos rulesets) — si el proyecto pasara a tener más de un desarrollador, restaurar esa exigencia.
- Usar siempre las CLIs de Aspire y de .NET para crear/gestionar proyectos (ver skills `aspire` y `dotnet` en `.claude/skills/`) — nunca escribir a mano ficheros de proyecto que esas herramientas generan.
- Solo SDK de .NET 10 **estable** — nunca preview (ver `global.json`).
- Las specs usan frontmatter de Obsidian en `camelCase` (`codigo`, `titulo`, `estado`, `autor`, `fechaCreacion`, `dependeDe`, `tags`) — plantillas completas en `docs/workflow.md`.

## Filosofía del proyecto

Proyecto voluntario, sin financiación, mantenido por una única persona. Priorizar siempre la solución más simple que cumpla los requisitos frente a abstracciones o sofisticación innecesarias — ver `docs/functional.md#recursos-de-desarrollo`.

## Idioma

Toda la documentación y comunicación del proyecto es en español.
