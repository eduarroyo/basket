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

### Iteración 2 (antes de fusionar el PR)

Con el PR #38 aún abierto (sin fusionar), el autor pidió una segunda pasada sobre el contenido:

- Añadir diapositivas de infraestructura de despliegue (GitHub Actions, Aspire como IaC, Azure) — antes solo aparecía como parte del diagrama de arquitectura, sin detalle propio.
- Quitar la diapositiva de caso de estudio BAS-3 (incidente de Managed Identity) — el autor consideró que no aportaba al objetivo de la presentación.
- Añadir diapositivas visibilizando cómo se ha usado la IA en el propio desarrollo: Claude Code multiagente vía `git worktree` (con el ejemplo real de los worktrees activos en el momento de escribir la presentación) y las skills a medida del proyecto (`.claude/skills/aspire`, `.claude/skills/dotnet`).
- Rebranding: título ajustado a "Máster en Desarrollo con IA" (BIGschool, curso real que motiva este TFC), con el logo de BIGschool (`img/bigschool-logo.svg`) en la portada, el cierre y un pie de página discreto en el resto de diapositivas.

**Logo de BIGschool**: descargado de `https://thebigschool.com/wp-content/uploads/2024/05/BigSchool-Logo.svg` (con permiso explícito del autor para guardarlo como copia local, más robusto que enlazarlo en caliente desde su web) a `img/bigschool-logo.svg`, sin modificar su contenido. El fichero de origen tiene un `viewBox` de `1500x572` pero el icono cuadrado "BIG" solo ocupa el tercio izquierdo (el resto del lienzo queda en blanco) — en vez de mostrar esa franja vacía, se recorta por CSS (`overflow: hidden` sobre un contenedor cuadrado) para mostrar solo el icono.

### Estructura de contenido final (16 diapositivas)

1. Portada (título, "Máster en Desarrollo con IA" · BIGschool, logo, autor, fecha)
2. Contexto/problema (competición de baloncesto base de Sevilla, proyecto voluntario sin financiación)
3. Qué hace la app — resumen funcional
4. Captura: portada pública + calendario
5. Captura: detalle de partido + clasificación
6. Arquitectura — diagrama Mermaid de despliegue
7. Decisiones de arquitectura destacadas (2-3: clasificación calculada al vuelo, caché con TTL sin purga activa, hosting serverless)
8. Infraestructura como código con .NET Aspire (`AppHost.cs`, `aspire run`/`aspire deploy`)
9. Despliegue continuo (ci.yml / publish.yml / deploy.yml, OIDC, revisiones múltiples, rollback)
10. Proceso: desarrollo dirigido por especificaciones (ciclo de `workflow.md`)
11. Claude Code multiagente: git worktrees (ejemplo real de los worktrees activos)
12. Skills a medida del proyecto (`.claude/skills/aspire`, `.claude/skills/dotnet`)
13. Gobernanza: ramas `feature/BAS-N`, PRs, CI obligatorio, ningún merge sin confirmación humana
14. Testing: tres niveles (unitario, integración con `Aspire.Hosting.Testing`, E2E Playwright)
15. Estado actual (19 incrementos completados) y cierre
16. Gracias

(El panel de administración se integró como parte de la diapositiva "Qué hace la app" en vez de una diapositiva propia, para dejar hueco a las nuevas sin perder el objetivo de mantener el bloque de producto compacto.)

### Validación local

Antes de dar por completado el incremento, arrancar `aspire run` en local y comprobar que `https://localhost:<puerto>/presentacion/` sirve la página correctamente (sin backend, solo confirma que el middleware de estáticos la expone bien).

### Fuera del plan

Sin tests automatizados nuevos — es contenido estático sin lógica de negocio ni endpoint propio (`architecture.md` punto 15: exige test para lógica de negocio y páginas/endpoints nuevos; esto no es ninguna de las dos cosas).
