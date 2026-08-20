---
codigo: BAS-20
estado: Borrador
tags:
  - tasks
---

# BAS-20: Tareas

- [ ] Añadir el enlace "Área de gestión" (`asp-area="Admin" asp-page="/Index"`) en un nuevo `<ul class="navbar-nav">` a la derecha de `Areas/Public/Pages/Shared/_Layout.cshtml`, sin comprobación de `User.Identity.IsAuthenticated`.
- [ ] Test de integración: vía cliente anónimo, la portada (`/`) contiene `href="/Admin">Área de gestión</a>`.
- [ ] Test de integración: vía cliente anónimo, seguir el enlace (`GET /Admin`) redirige a `/Admin/Login` (mismo patrón que `PeticionAnonimaATemporadasRedirigeALogin` en `CatalogoAdminPagesTests.cs`).
- [ ] Test de integración: vía cliente autenticado (`LoginAsync`), `GET /Admin` llega directo a `Admin/Index` sin pasar por login.
- [ ] Verificación manual en `aspire run` (o Claude in Chrome): clic en "Área de gestión" sin sesión iniciada → login → tras autenticar, aterriza en el panel; repetir con sesión ya iniciada → aterriza directo en el panel.
- [ ] Revisar la Definición de Hecho (`workflow.md`) antes de abrir el PR a `develop`.
