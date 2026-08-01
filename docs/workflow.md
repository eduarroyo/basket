# Flujo de trabajo (Spec-Driven Development)

Este fichero describe cómo se planifica e implementa cada incremento de BasketBaseTracker: desarrollo dirigido por especificaciones (SDD), sin herramienta externa — solo estructura de carpetas, plantillas y disciplina de conversación con el asistente de IA.

No sustituye a [`functional.md`](./functional.md), [`data-model.md`](./data-model.md), [`screens.md`](./screens.md) y [`architecture.md`](./architecture.md): esos documentos son la referencia fija ("constitución" del proyecto) que consulta cada incremento. Este fichero describe el *proceso*, no el *qué*.

## Nomenclatura

Cada incremento tiene un código secuencial **`BAS-N`** (BasketBaseTracker), asignado una vez y nunca reutilizado, incluso si el incremento se archiva o se descarta. Sirve para referenciarlo en commits, ramas y PRs (`BAS-2: competiciones y equipos`).

## Estructura de carpetas

```text
specs/
  BAS-1/
    spec.md      # qué y por qué
    plan.md      # cómo
    tasks.md     # tareas concretas y verificables
  BAS-2/
    ...
  archive/
    BAS-1/       # incrementos completados se mueven aquí
```

El proyecto se lleva como vault de Obsidian: los enlaces `[[...]]` entre specs y hacia `data-model.md`/`screens.md`/`architecture.md` permiten visualizar las dependencias entre incrementos en el grafo. Esto es un beneficio adicional, no un requisito — los ficheros son markdown plano y funcionan igual sin Obsidian.

## Ramas de Git

- `main` — rama estable, desplegable a producción.
- `develop` — rama de integración; los incrementos completados se fusionan aquí antes de pasar a `main`.
- `feature/<código>` — una rama por incremento (p. ej. `feature/BAS-2`), creada desde `develop`, sin sufijo descriptivo adicional.

Ciclo de vida de una rama de incremento:

1. Al empezar a trabajar en `BAS-N` (inicio de la fase de propuesta), se crea `feature/BAS-N` desde `develop`.
2. Todo el trabajo del incremento —`spec.md`, `plan.md`, `tasks.md` y la implementación— se realiza en esa rama.
3. Al completarse el incremento, se abre un Pull Request de `feature/BAS-N` a `develop`.
4. La rama **solo se fusiona y se cierra con confirmación humana explícita** — el asistente de IA nunca fusiona el PR ni elimina la rama por su cuenta, aunque todas las tareas de `tasks.md` estén completas.

## Ciclo por incremento

1. **Propuesta (`spec.md`)** — se crea la rama `feature/BAS-N` desde `develop` (ver "Ramas de Git"). Qué se va a construir y por qué, alcance, historias de usuario, criterios de aceptación. Se redacta en conversación con el asistente.
2. **Aclaración** — antes de planificar, ronda explícita de preguntas para eliminar ambigüedad. Se documenta en la sección `## Aclaraciones` del propio `spec.md`.
3. **Plan técnico (`plan.md`)** — qué entidades de `data-model.md` y qué pantallas de `screens.md` toca, y decisiones técnicas específicas del incremento no cubiertas ya por `architecture.md`.
4. **Tareas (`tasks.md`)** — lista ordenada de tareas pequeñas y verificables (checkbox).
5. **Implementación** — se ejecutan las tareas una a una, marcando checkboxes conforme se completan.
6. **Cierre** — al completarse, la carpeta se mueve a `specs/archive/`; si el incremento reveló cambios de diseño no anticipados, se actualizan `data-model.md`/`screens.md`/`architecture.md` antes de archivar. Se abre el PR de `feature/BAS-N` a `develop` (ver "Ramas de Git") y se espera confirmación humana para fusionarlo.

## Estado de una spec

`Borrador` → `En aclaración` → `Planificado` → `En implementación` → `Completado` → `Archivado` (o `Descartado` en cualquier punto).

## Plantillas

### `spec.md`

```markdown
---
codigo: BAS-N
titulo: Título del incremento
estado: Borrador
autor: Eduardo Arroyo
fechaCreacion: AAAA-MM-DD
dependeDe:
  - "[[BAS-X/spec|BAS-X]]"
tags:
  - backend
---

# BAS-N: Título del incremento

## Descripción
...

## Alcance
...

## Fuera de alcance
...

## Criterios de aceptación
- [ ] ...

## Aclaraciones
...

## Referencias
- [[data-model]] — entidades ...
- [[screens]] — pantallas ...
- [[architecture]] — decisiones ...
```

`dependeDe` se omite si el incremento no depende de ningún otro. `tags` describe el tipo de trabajo (`backend`, `frontend`, `documentación`...), no que sea una spec — eso ya lo indica su ubicación en `specs/`.

### `plan.md`

```markdown
---
codigo: BAS-N
estado: Borrador
tags:
  - plan
---

# BAS-N: Plan técnico

## Entidades del modelo de datos afectadas
...

## Pantallas afectadas
...

## Decisiones técnicas específicas de este incremento
...
```

### `tasks.md`

```markdown
---
codigo: BAS-N
estado: Borrador
tags:
  - tasks
---

# BAS-N: Tareas

- [ ] Tarea 1
- [ ] Tarea 2
```
