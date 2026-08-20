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
- [ ] Cierre: mover `docs/specs/BAS-19/` a `docs/specs/archive/BAS-19/`, actualizar estado a `Completado` y abrir el PR de `feature/BAS-19` a `develop` (sin fusionar sin confirmación humana)
