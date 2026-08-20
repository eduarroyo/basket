---
codigo: BAS-18
titulo: Corrección de bugs de navegación (portada y clasificación pública)
estado: Completado
autor: Eduardo Arroyo
fechaCreacion: 2026-08-20
tags:
  - frontend
---

# BAS-18: Corrección de bugs de navegación (portada y clasificación pública)

## Descripción

Tres bugs de navegación detectados en producción, todos relacionados con enlaces mal construidos o ausentes:

1. En el layout del área pública (`Areas/Public/Pages/Shared/_Layout.cshtml`), tanto el título de la barra superior ("BasketBaseTracker") como el botón "Inicio" usan `asp-area="" asp-page="/Index"`. Como no existe ninguna página `/Index` fuera de un área, el enlace no resuelve a la portada real (`Areas/Public/Pages/Index.cshtml`, área `Public`) y el usuario se queda en la página actual.
2. La página pública de clasificación (`Areas/Public/Pages/Competiciones/Clasificacion.cshtml`) no tiene ningún enlace de vuelta, a diferencia del resto de páginas del área pública (Calendario, Jornada, Partido, Equipo...), que sí siguen el patrón "Volver a...".
3. El botón "Ver web pública" del layout del área Admin (`Areas/Admin/Pages/Shared/_Layout.cshtml`) tiene el mismo problema que el punto 1 (`asp-area="" asp-page="/Index"`): no lleva a la portada pública, sino que se queda en la página actual del panel de administración.

Los puntos 1 y 3 comparten la misma causa raíz: el resto del código del área pública usa consistentemente `asp-area="Public"` para enlazar páginas de esa área (ver por ejemplo `Temporadas/Competiciones.cshtml`, `Competiciones/Calendario.cshtml`...) — la convención de rutas de `Program.cs` (`AddAreaFolderRouteModelConvention`) quita el prefijo `/Public` de la URL, pero la página sigue perteneciendo lógicamente al área `Public` a efectos de `asp-page`/`asp-area`.

## Alcance

- Corregir el enlace del título y del botón "Inicio" en `Areas/Public/Pages/Shared/_Layout.cshtml` para que apunten a la portada pública.
- Corregir el enlace "Ver web pública" en `Areas/Admin/Pages/Shared/_Layout.cshtml` para que apunte a la portada pública.
- Añadir un enlace "Volver a competiciones" en `Areas/Public/Pages/Competiciones/Clasificacion.cshtml`, siguiendo el mismo patrón que `Competiciones/Calendario.cshtml` (vuelve a `Temporadas/Competiciones` de la temporada de la competición).

## Fuera de alcance

- Cualquier otro enlace de navegación no mencionado arriba (no se ha detectado ningún otro caso de `asp-area=""` mal usado en el resto del código).
- Cambios de diseño/estilo de la barra de navegación o del layout.

## Criterios de aceptación

- [x] Al hacer clic en el título "BasketBaseTracker" de la barra superior del área pública, el usuario navega a la portada (`/`).
- [x] Al hacer clic en "Inicio" en la barra superior del área pública, el usuario navega a la portada (`/`).
- [x] La página pública de clasificación de una competición muestra un enlace "Volver a competiciones" que navega a `Temporadas/Competiciones` de la temporada correspondiente.
- [x] Al hacer clic en "Ver web pública" desde cualquier página del área Admin, el usuario navega a la portada pública (`/`), no a la página actual del panel.
- [x] Test de integración que cubra la generación correcta de estos tres enlaces (no solo verificación manual).

## Aclaraciones

- **¿A dónde debe volver el enlace de la página de clasificación?** A `Temporadas/Competiciones` de la temporada de la competición — es el mismo destino que usa el enlace "Volver a competiciones" ya existente en `Competiciones/Calendario.cshtml`, y es la página desde la que normalmente se llega a la clasificación (lista de competiciones de una temporada, con enlaces "Calendario" y "Ver clasificación" uno junto a otro). Se mantiene así la consistencia ya establecida en el resto de páginas del área pública, sin necesidad de introducir un patrón nuevo.

## Referencias
- [[screens]] — pantallas "Clasificación" (pública) y layouts de las áreas Public/Admin.
- [[archive/BAS-3/spec|BAS-3]] — introdujo la convención de rutas del área Public sin prefijo (`Program.cs`).
