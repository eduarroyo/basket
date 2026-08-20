---
codigo: BAS-20
estado: Completado
tags:
  - tasks
---

# BAS-20: Tareas

- [x] Añadir el enlace "Área de gestión" (`asp-area="Admin" asp-page="/Index"`) en un nuevo `<ul class="navbar-nav">` a la derecha de `Areas/Public/Pages/Shared/_Layout.cshtml`, sin comprobación de `User.Identity.IsAuthenticated`.
- [x] Test de integración: vía cliente anónimo, la portada (`/`) contiene `href="/Admin">Área de gestión</a>`.
- [x] Test de integración: vía cliente anónimo, seguir el enlace (`GET /Admin`) redirige a `/Admin/Login` (mismo patrón que `PeticionAnonimaATemporadasRedirigeALogin` en `CatalogoAdminPagesTests.cs`).
- [x] Test de integración: vía cliente autenticado (`LoginAsync`), `GET /Admin` llega directo a `Admin/Index` sin pasar por login.
- [x] Verificación manual en `aspire run` (Claude in Chrome): clic en "Área de gestión" sin sesión iniciada → login (`/Admin/Login?ReturnUrl=%2FAdmin`) → tras autenticar, aterriza en `/Admin`; repetido con sesión ya iniciada (tras "Ver web pública") → aterriza directo en `/Admin` sin pasar por login.
- [x] Revisar la Definición de Hecho (`workflow.md`) antes de abrir el PR a `develop`.
