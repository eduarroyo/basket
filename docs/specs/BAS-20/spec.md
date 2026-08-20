---
codigo: BAS-20
titulo: Acceso al área de gestión desde el layout público
estado: Planificado
autor: Eduardo Arroyo
fechaCreacion: 2026-08-20
tags:
  - frontend
---

# BAS-20: Acceso al área de gestión desde el layout público

## Descripción

Actualmente no hay ninguna forma de llegar al área de administración (`/Admin`) desde la parte pública del sitio salvo que el usuario conozca o teclee la URL directamente. Se añade un enlace discreto "Área de gestión" en el hueco derecho de la barra de navegación pública (`Areas/Public/Pages/Shared/_Layout.cshtml`), simétrico al bloque de la derecha que ya existe en el layout del área Admin (`Areas/Admin/Pages/Shared/_Layout.cshtml`, con "Ver web pública" / "Cerrar sesión").

El enlace apunta directamente a `/Admin` (la portada del panel, `Admin/Index`), sin comprobar el estado de autenticación en el propio layout: el mecanismo de cookie auth ya configurado en `Program.cs` (`options.LoginPath = "/Admin/Login"`) intercepta cualquier petición no autenticada a `/Admin` y redirige a `Admin/Login` con el `ReturnUrl` correspondiente; `LoginModel` ya devuelve a `~/Admin` tras un login correcto. No se requiere ningún cambio de backend — es un incremento puramente de plantilla/frontend.

## Alcance

- Añadir un `<ul class="navbar-nav">` a la derecha del navbar de `Areas/Public/Pages/Shared/_Layout.cshtml`, con un único enlace de texto "Área de gestión" (`asp-area="Admin" asp-page="/Index"`).
- El enlace es estático: se muestra igual para usuarios autenticados y no autenticados (no se añade ningún `@if (User.Identity?.IsAuthenticated ...)` en el layout público), para no romper el Output Caching de las páginas públicas (`architecture.md`, punto 5).
- Estilo visual discreto, consistente con el bloque derecho ya existente en el layout admin (texto plano, no botón destacado).

## Fuera de alcance

- Cualquier cambio en el mecanismo de autenticación, `LoginPath`, o el flujo de `ReturnUrl` — ya funcionan correctamente y no se tocan.
- Cambios en el layout del área Admin.
- Iconografía (candado/engranaje) — se descarta a favor de texto plano, ver Aclaraciones.

## Criterios de aceptación

- [ ] En cualquier página del área pública, la barra de navegación muestra un enlace "Área de gestión" a la derecha.
- [ ] Al hacer clic en el enlace sin sesión iniciada, el usuario llega a `Admin/Login` y, tras autenticarse, es redirigido a `Admin/Index`.
- [ ] Al hacer clic en el enlace con sesión ya iniciada (p. ej. tras volver a la parte pública desde "Ver web pública" del admin), el usuario llega directamente a `Admin/Index` sin pasar por login.
- [ ] El enlace es idéntico (mismo HTML) para usuarios autenticados y no autenticados — no depende de `User.Identity.IsAuthenticated` en el layout público.
- [ ] Test de integración que cubra la generación del enlace y la redirección a login para un cliente no autenticado.

## Aclaraciones

- **¿Qué texto debe llevar el enlace?** "Área de gestión" — se descartó "Gestionar" (ambiguo para un visitante anónimo sin contexto: "¿gestionar qué?") y un icono sin texto (menos accesible, y requeriría elegir librería de iconos sin que el proyecto use ninguna actualmente). "Área de gestión" es suficientemente explícito sin usar la palabra "admin", y de longitud similar a "Ver web pública" del layout admin.
- **¿Debe cambiar el texto o la visibilidad del enlace si el usuario ya tiene sesión iniciada?** No — el mismo texto y el mismo destino (`/Admin`) sirven para ambos casos, ya que el propio middleware de autenticación decide si deja pasar directamente o redirige a login. Mantener el layout público sin lógica condicional de autenticación evita tener que excluirlo del Output Caching de las páginas públicas.

## Referencias
- [[screens]] — layouts de las áreas Public/Admin.
- [[architecture]] — punto 5 (Output Caching de páginas públicas), punto 7 (roles y autenticación).
- [[archive/BAS-18/spec|BAS-18]] — corrigió enlaces de navegación en los mismos dos layouts (Public y Admin).
