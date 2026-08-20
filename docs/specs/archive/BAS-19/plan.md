---
codigo: BAS-19
estado: Completado
tags:
  - plan
---

# BAS-19: Plan técnico

## Entidades del modelo de datos afectadas

Ninguna. No hay cambios en `data-model.md`.

## Pantallas afectadas

Ninguna de `screens.md` (no es una pantalla de producto). Se añade una página nueva fuera de ese inventario: `/presentacion/` (fichero estático, sin controlador ni autenticación).

## Decisiones técnicas específicas de este incremento

### Ubicación e implementación

- `src/BasketBaseTracker.Web/wwwroot/presentacion/index.html` — página única, autocontenida (reveal.js embebido o vía CDN con fallback razonable, CSS/JS inline donde sea práctico para minimizar dependencias externas).
- Imágenes de las capturas en `src/BasketBaseTracker.Web/wwwroot/presentacion/img/` (ficheros `.png`/`.webp`), referenciadas con rutas relativas — no como `data:` URI, para no inflar un único HTML y poder cachear las imágenes por separado.
- Sin controlador ni Razor Page: se sirve como fichero estático de `wwwroot`.
- **Corregido durante la validación local**: el proyecto no usa `UseStaticFiles()` clásico, sino `app.MapStaticAssets()` (pipeline de activos estáticos de ASP.NET Core 9/10), que sirve ficheros individuales pero no un documento por defecto para una ruta de directorio (`/presentacion/` devolvía 404; `/presentacion/index.html` sí funcionaba). Se añade `app.UseDefaultFiles()` justo antes de `app.MapStaticAssets()` en `Program.cs` — middleware estándar de ASP.NET Core, sin lógica de negocio ni dependencia nueva, que resuelve `index.html` para cualquier ruta de directorio bajo `wwwroot` (no solo `/presentacion/`). Sí se toca `Program.cs`, a diferencia de lo previsto inicialmente en este plan.
- Sin enlace desde la navegación pública del sitio (header/footer de `Public`) — accesible solo conociendo la URL directa, que es justo lo que necesita el evaluador.

### Capturas de pantalla

Se navegan con automatización de navegador contra `https://web.ashymoss-b1c8f995.spaincentral.azurecontainerapps.io/` (producción), páginas públicas únicamente:

- Portada (`/`)
- Calendario de una competición con partidos jugados
- Clasificación de esa misma competición
- Detalle de un partido o resultados por jornada (la que tenga datos más ilustrativos)

Sin login en el área Admin de producción (ver "Fuera de alcance" en `spec.md`) — esa diapositiva usa el inventario de `screens.md` descrito en texto, no una captura.

### Diagrama de arquitectura

Se reutiliza como base el diagrama Mermaid ya existente en `architecture.md` (sección "Diagrama de despliegue"), simplificado si hace falta para legibilidad en una diapositiva. reveal.js soporta Mermaid de forma nativa vía el plugin oficial (`RevealMermaid`) cargado desde CDN — se usa esa vía en vez de exportar el diagrama como imagen estática, para no duplicar mantenimiento entre `architecture.md` y la presentación.

### Caso de estudio del proceso SDD

Se usa `BAS-3` (despliegue a producción) como ejemplo: ilustra el ciclo completo spec→plan→tasks→implementación→cierre y un caso real de decisión revisada durante la implementación (Managed Identity para Azure SQL revertida a autenticación SQL tras un bug de plataforma descubierto en el primer despliegue) — buen ejemplo de que el proceso no es solo "generar código", incluye descubrir y documentar por qué un plan inicial cambia.

### Estructura de contenido (12-15 diapositivas)

1. Portada (título, subtítulo "trabajo de fin de curso — desarrollo con IA", autor, fecha)
2. Contexto/problema (competición de baloncesto base de Sevilla, proyecto voluntario sin financiación)
3. Qué hace la app — resumen funcional
4. Captura: portada pública
5. Captura: calendario / resultados
6. Captura: clasificación
7. Panel de administración (descripción + inventario de `screens.md`, sin captura)
8. Arquitectura — diagrama Mermaid de despliegue
9. Decisiones de arquitectura destacadas (2-3: clasificación calculada al vuelo, caché con TTL sin purga activa, hosting serverless)
10. Proceso: desarrollo dirigido por especificaciones (ciclo de `workflow.md`)
11. Caso de estudio: BAS-3 (spec → decisión revisada en implementación → cierre)
12. Gobernanza: ramas `feature/BAS-N`, PRs, CI obligatorio, ningún merge sin confirmación humana
13. Testing: tres niveles (unitario, integración con `Aspire.Hosting.Testing`, E2E Playwright)
14. Estado actual (18 incrementos completados) y cierre

### Validación local

Antes de dar por completado el incremento, arrancar `aspire run` en local y comprobar que `https://localhost:<puerto>/presentacion/` sirve la página correctamente (sin backend, solo confirma que el middleware de estáticos la expone bien).

### Fuera del plan

Sin tests automatizados nuevos — es contenido estático sin lógica de negocio ni endpoint propio (`architecture.md` punto 15: exige test para lógica de negocio y páginas/endpoints nuevos; esto no es ninguna de las dos cosas).
