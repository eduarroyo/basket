---
codigo: BAS-1
estado: Archivado
tags:
  - tasks
---

# BAS-1: Tareas

- [x] Redactar `functional.md` (funcionalidades, NFR, recursos de desarrollo, usuarios del sistema).
- [x] Redactar `data-model.md` (entidades, relaciones, notas de diseño).
- [x] Redactar `screens.md` (inventario de pantallas públicas y admin).
- [x] Redactar `architecture.md` (decisiones de arquitectura 1-13 iniciales).
- [x] Redactar `workflow.md` (ciclo de spec-driven development).
- [x] Detallar estrategia de pruebas: xUnit v3, unitarios/integración/E2E, cuándo se ejecutan (`architecture.md` punto 15).
- [x] Detallar observabilidad: logs, trazas, métricas, alertas, disponibilidad (`architecture.md` punto 8).
- [x] Detallar caché y su invalidación: TTL vs. purga activa, Cache Rule de Cloudflare (`architecture.md` punto 5).
- [x] Resolver el recálculo/cálculo de la clasificación: de tabla materializada a consulta calculada al vuelo, por el problema de concurrencia detectado (`architecture.md` punto 14).
- [x] Detallar seguridad operativa: umbrales de rate limiting, política de contraseñas/bloqueo, provisión del primer administrador (`architecture.md` puntos 6-7).
- [x] Detallar importación/exportación: modo reemplazo completo, salvaguardas, versionado de esquema (`architecture.md` punto 10).
- [x] Resolver el formato de competición y las fases finales: `Jornada.Etiqueta` + `Jornada.CuentaParaClasificacion` en vez de un motor de brackets (`architecture.md` punto 16).
- [x] Resolver la política de retención de datos personales (`architecture.md` punto 17).
- [x] Consultar y resumir las Reglas Oficiales de Baloncesto FIBA 2022, el Reglamento General y de Competiciones de la F.A.B. y el Reglamento Disciplinario de la F.A.B. (`docs/reglamento/resumen-reglas-relevantes.md`).
- [x] Modelar `PenalizacionClasificacion` a partir de lo encontrado en el Reglamento Disciplinario (Art. 43).
- [x] Documentar el procedimiento para alineación indebida sin mala fe usando `Partido.Observaciones` en vez de un modelo nuevo.
- [x] Registrar en `## Mejoras futuras` de `architecture.md` las alternativas descartadas (MF-1 a MF-7).
- [x] Verificar que no quedan entradas en "Pendiente de definir" de `data-model.md` ni de `architecture.md` (salvo la validación de GHCR/Aspire al implementar, que es un riesgo a vigilar, no una decisión pendiente).
- [x] Crear esta spec retroactivamente en `docs/specs/archive/BAS-1/` para dejar constancia en el histórico.
- [x] Mover `specs/` a `docs/specs/` para que quede dentro de la bóveda de Obsidian (raíz `docs/`).
