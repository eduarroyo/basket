---
codigo: BAS-18
estado: Completado
tags:
  - plan
---

# BAS-18: Plan técnico

## Entidades del modelo de datos afectadas

Ninguna — solo se toca la navegación (enlaces) de páginas ya existentes, sin cambios de datos.

## Pantallas afectadas

- Layout del área pública (`Areas/Public/Pages/Shared/_Layout.cshtml`).
- Layout del área Admin (`Areas/Admin/Pages/Shared/_Layout.cshtml`).
- Clasificación pública (`Areas/Public/Pages/Competiciones/Clasificacion.cshtml` y su `PageModel`).

## Decisiones técnicas específicas de este incremento

1. **Bugs 1 y 3 (misma causa raíz)**: cambiar `asp-area="" asp-page="/Index"` por `asp-area="Public" asp-page="/Index"` en los tres enlaces afectados (título + "Inicio" en el layout público, "Ver web pública" en el layout Admin). No requiere cambios en `Program.cs` ni en la convención de rutas — es un error de uso del tag helper, no de la convención en sí (que ya funciona correctamente para el resto de enlaces del área pública, todos con `asp-area="Public"` explícito).

2. **Bug 2**: `ClasificacionModel` solo recibe `id` (el id de la competición) y delega en `ClasificacionService`, que no expone la temporada. Para poder generar el enlace "Volver a competiciones" (que necesita el `TemporadaId`), `ClasificacionModel` inyecta además `ApplicationDbContext` y añade una consulta mínima (`FindAsync` o `Select` proyectado a `TemporadaId`) — mismo patrón que ya usa `CalendarioModel`, que inyecta `ApplicationDbContext` directamente en vez de a través de un servicio. No se modifica `ClasificacionService` (su contrato es compartido con la vista admin de verificación, sin caché — no debe cambiar).

3. **Tests**: se añade un test de integración (`BasketBaseTracker.Tests`, proyecto de integración existente) que, contra el `WebApplicationFactory`/`Aspire.Hosting.Testing` ya usado por el resto de tests del área pública, comprueba que el HTML devuelto por cada página contiene el `href` esperado (`/`  para portada, ruta de `Temporadas/Competiciones` para el enlace de clasificación) — no solo que la página cargue. Se sigue el patrón de test ya existente para páginas del área pública (buscar el fichero de test de `Competiciones/Calendario.cshtml` o similar como referencia de estructura).
