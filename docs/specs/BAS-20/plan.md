---
codigo: BAS-20
estado: Completado
tags:
  - plan
---

# BAS-20: Plan técnico

## Entidades del modelo de datos afectadas

Ninguna — solo se toca la navegación (un enlace) del layout público, sin cambios de datos.

## Pantallas afectadas

- Layout del área pública (`Areas/Public/Pages/Shared/_Layout.cshtml`) — al ser el layout compartido, afecta a las 11 pantallas públicas de `screens.md`, aunque el cambio en sí es un único fichero.

## Decisiones técnicas específicas de este incremento

1. **Marcado**: añadir un segundo `<ul class="navbar-nav">` dentro del `div.navbar-collapse` ya existente en `_Layout.cshtml`, a continuación del `<ul>` de "Inicio" (mismo patrón de dos `<ul>` — uno a la izquierda, otro a la derecha — que ya usa `Areas/Admin/Pages/Shared/_Layout.cshtml`). Enlace único: `asp-area="Admin" asp-page="/Index"`, texto "Área de gestión". Se usa `asp-area="Admin"` explícito (no `asp-area=""`), evitando el error corregido en BAS-18.
2. **Resolución de URL**: `asp-area="Admin" asp-page="/Index"` resuelve a `href="/Admin"` — el área Admin conserva su prefijo de ruta por defecto (`Program.cs`, a diferencia de `Public`, que lo pierde por convención propia) y la página `Index` omite su nombre de la URL canónica, igual que ya hace `LoginModel` al usar `Url.Content("~/Admin")` como `ReturnUrl` por defecto.
3. **Sin lógica de autenticación en el layout público**: a diferencia del layout Admin, no se añade ningún `@if (User.Identity?.IsAuthenticated ...)` — decisión ya tomada en `spec.md` para mantener el HTML idéntico con o sin sesión y no interferir con el Output Caching (`architecture.md`, punto 5).
4. **Tests**: se amplía `PublicPagesTests.cs` (o clase de test equivalente ya existente para el área Admin, `CatalogoAdminPagesTests.cs`), reutilizando dos patrones ya presentes en el proyecto:
   - Igual que el test de BAS-18 para "Ver web pública" (`Assert.Contains("""href="/Admin">Área de gestión</a>""", html)`), contra un cliente anónimo en la portada.
   - Igual que `PeticionAnonimaATemporadasRedirigeALogin` de `CatalogoAdminPagesTests.cs` (`response.RequestMessage?.RequestUri?.AbsolutePath == "/Admin/Login"`), siguiendo el enlace con un cliente anónimo.
   - Caso adicional con cliente autenticado (`LoginAsync`, ya usado en el resto de tests de integración): `GET /Admin` debe llegar directo a `Admin/Index` sin redirección a login.
