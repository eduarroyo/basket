---
codigo: BAS-18
estado: Completado
tags:
  - tasks
---

# BAS-18: Tareas

- [x] Corregir `asp-area="" asp-page="/Index"` → `asp-area="Public" asp-page="/Index"` en el título y el enlace "Inicio" de `Areas/Public/Pages/Shared/_Layout.cshtml`.
- [x] Corregir `asp-area="" asp-page="/Index"` → `asp-area="Public" asp-page="/Index"` en el enlace "Ver web pública" de `Areas/Admin/Pages/Shared/_Layout.cshtml`.
- [x] Añadir `ApplicationDbContext` a `ClasificacionModel` (mismo patrón que `CalendarioModel`) y cargar el `TemporadaId` de la competición; añadir en `Clasificacion.cshtml` el enlace "Volver a competiciones" hacia `Temporadas/Competiciones` con ese id, con el mismo estilo que el de `Competiciones/Calendario.cshtml`.
- [x] Test de integración en `PublicPagesTests.cs`: la portada (`/`) contiene un enlace `href="/"` en el título/"Inicio" del layout (o, equivalente, que el layout no genera un `href` roto).
- [x] Test de integración en `PublicPagesTests.cs` (o `AdminHttpTestHelpers`/página admin existente): una página del área Admin contiene el enlace "Ver web pública" con `href="/"`.
- [x] Test de integración en `ClasificacionPagesTests.cs`: la página pública de clasificación contiene el enlace "Volver a competiciones" con `href="/temporadas/{temporadaId}/competiciones"`.
- [x] Verificación manual en `aspire run`: clic en el título y en "Inicio" desde varias páginas públicas, clic en "Ver web pública" desde el panel admin, clic en "Volver a competiciones" desde la clasificación — confirmar que cada uno navega al destino correcto. Hecho en navegador real (Claude in Chrome): título/"Inicio" navegan a `/`, "Ver web pública" navega a `/`, "Volver a competiciones" navega a `/temporadas/1/competiciones` — los tres con clic real, no solo comprobación del atributo `href`.
- [x] Revisar la Definición de Hecho (`workflow.md`) antes de abrir el PR a `develop`.
