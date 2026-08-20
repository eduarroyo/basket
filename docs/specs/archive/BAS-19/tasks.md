---
codigo: BAS-19
estado: Completado
tags:
  - tasks
---

# BAS-19: Tareas

- [x] Navegar `https://web.ashymoss-b1c8f995.spaincentral.azurecontainerapps.io/` y capturar: portada, calendario/resultados de una competición con datos, clasificación
- [x] Guardar las capturas en `src/BasketBaseTracker.Web/wwwroot/presentacion/img/`
- [x] Crear `src/BasketBaseTracker.Web/wwwroot/presentacion/index.html` con reveal.js (CDN) y el plugin Mermaid
- [x] Redactar el contenido de las 12-15 diapositivas según la estructura de `plan.md`
- [x] Incluir el diagrama de arquitectura (Mermaid, adaptado de `architecture.md`)
- [x] Incluir el caso de estudio BAS-3 con datos reales de su spec archivada
- [x] Añadir la carpeta `docs/specs/BAS-19/` al `BasketBaseTracker.slnx`
- [x] Validar en local con `aspire run` que `/presentacion/` sirve la página correctamente (encontrado y corregido un bug de orden del pipeline: `UseDefaultFiles()` debe ir antes de `UseRouting()`)
- [x] Ejecutar la suite completa de tests (110, unitarios + integración) para confirmar que el cambio en `Program.cs` no rompe nada
- [x] Cierre: mover `docs/specs/BAS-19/` a `docs/specs/archive/BAS-19/`, actualizar estado a `Completado` y abrir el PR de `feature/BAS-19` a `develop` (sin fusionar sin confirmación humana)

## Iteración 2 (con el PR #38 abierto, antes de fusionar)

- [x] Añadir diapositivas de infraestructura como código (.NET Aspire) y de despliegue continuo (GitHub Actions, OIDC, revisiones múltiples)
- [x] Quitar la diapositiva de caso de estudio BAS-3 (Managed Identity)
- [x] Añadir diapositiva de Claude Code multiagente con git worktrees (con el ejemplo real de `git worktree list`)
- [x] Añadir diapositiva de skills a medida del proyecto (`.claude/skills/aspire`, `.claude/skills/dotnet`)
- [x] Pedir permiso y descargar el logo de BIGschool a `img/bigschool-logo.svg`; corregir por CSS el recorte del lienzo vacío del fichero de origen
- [x] Ajustar título/subtítulo a "Máster en Desarrollo con IA" · BIGschool
- [x] Revalidar en local con `aspire run` (capturas de cada diapositiva nueva) tras un fallo transitorio de puerto en el primer intento

## Iteración 3 (con el PR #38 abierto, antes de fusionar)

- [x] Quitar el framing "proyecto voluntario, sin financiación" de la diapositiva de contexto; reformular como Trabajo de Fin de Máster con vocación de convertirse en herramienta real
- [x] Añadir diapositiva "Mejoras pendientes" (BAS-4, UX/interfaz, alta de administradores, mejoras futuras registradas en `architecture.md`)
- [x] Mejorar la diapositiva de arquitectura: lista de tecnologías con viñetas (antes en una sola línea) + nota de simplicidad deliberada
- [x] Renombrar y reforzar la diapositiva de CI/CD: fases etiquetadas (Integración/Publicación/Despliegue) + línea de tecnologías
- [x] Diagnosticar y corregir el bug de "recuadro vacío" en el diagrama Mermaid: renderizar cada diagrama al hacerse visible su diapositiva (`slidechanged`), no todos de golpe al cargar con las demás diapositivas ocultas (`display:none` rompe `getBBox()`)
- [x] Revalidar navegando la presentación de principio a fin con clics reales (no solo saltos de hash) para reproducir fielmente el escenario del bug
