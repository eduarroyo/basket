---
codigo: BAS-6
titulo: Área Admin — catálogo (Temporadas, Categorías, Clubes, Sedes)
estado: Planificado
autor: Eduardo Arroyo
fechaCreacion: 2026-08-18
dependeDe:
  - "[[archive/BAS-5/spec|BAS-5]]"
tags:
  - backend
  - frontend
---

# BAS-6: Área Admin — catálogo (Temporadas, Categorías, Clubes, Sedes)

## Descripción

BAS-5 dejó el modelo de datos de dominio implementado (entidades EF Core + migración), pero sin ninguna pantalla que lo use. Este incremento añade las primeras pantallas reales del área Admin: las cuatro de catálogo de `screens.md` (`Temporadas`, `Categorías`, `Clubes`, `Sedes`), las más simples porque ninguna de las cuatro tiene claves foráneas — no dependen de que exista ninguna otra pantalla primero. Son también la base sobre la que colgarán las pantallas de "Gestión anual" (Competiciones, Equipos...) de incrementos futuros, que sí referencian estas cuatro entidades.

## Alcance

- Listado, alta y edición (Razor Pages, scaffolding `dotnet aspnet-codegenerator razorpage`) para `Temporada`, `Categoria`, `Club` y `Sede`, en `Areas/Admin/Pages/<Entidad>/`.
- Validación server-side acorde a las restricciones ya declaradas en la configuración de EF Core de BAS-5 (longitudes máximas, campos obligatorios) — sin reglas de negocio nuevas.
- `Temporada.Estado` editable como campo de formulario normal (desplegable del enum), sin máquina de estados ni restricciones de transición — `functional.md` no impone ninguna regla al respecto.
- Layout propio del área Admin (`Areas/Admin/Pages/Shared/_Layout.cshtml`), con navegación mínima a las cuatro pantallas y enlace de cierre de sesión — hasta ahora el área Admin reutilizaba el layout público sin ningún menú propio.
- Página de inicio del área Admin (`Areas/Admin/Pages/Index.cshtml`) con enlaces a las cuatro pantallas — necesaria porque tras iniciar sesión no hay ningún punto de entrada al área Admin todavía.
- Tests de integración: alta, edición y listado por entidad, más verificación de que una petición sin autenticar a cualquiera de estas pantallas redirige a `/Admin/Login`.

## Fuera de alcance

- Borrado — ninguna de las cuatro pantallas de catálogo lo incluye en `screens.md` (solo "Listado / alta / edición"); evita además dejar huérfanas las entidades que ya referencian catálogo con `Restrict` (BAS-5).
- Pantallas de "Gestión anual" (`Competiciones`, `Equipos`, `Plantilla de equipo`, `Calendario`, `Resultados`, `Clasificación`, `Penalizaciones de clasificación`) — dependen de que exista este catálogo primero; incrementos siguientes.
- `Importación/exportación` y `Usuarios/roles` — placeholders explícitos en `screens.md`, no forman parte de v1.
- Cualquier pantalla del área pública — sigue sin datos de catálogo que mostrar hasta que existan competiciones reales.
- Diseño visual más allá del Bootstrap por defecto de la plantilla — sin sistema de diseño propio.

## Criterios de aceptación

- [ ] Un administrador autenticado puede listar, dar de alta y editar `Temporada`, `Categoria`, `Club` y `Sede` desde `/Admin`.
- [ ] Los formularios de alta/edición validan las restricciones del modelo (campos obligatorios, longitud máxima) y muestran los errores correspondientes sin guardar si no se cumplen.
- [ ] Una petición sin autenticar a cualquiera de las dieciséis páginas (4 entidades × Index/Create/Edit + Index del área, más las ya existentes Login/Logout) redirige a `/Admin/Login`, salvo Login/Logout.
- [ ] Existe un test de integración por entidad que cubre alta + edición + listado, y al menos un test que verifica la redirección a Login sin autenticar.
- [ ] La rama compila y todos los tests (unitarios + integración) pasan en CI.

## Aclaraciones

- **¿Por qué sin borrado si `screens.md` no lo prohíbe explícitamente?** → `screens.md` describe estas cuatro pantallas como "Listado / alta / edición" en su columna de descripción, sin mencionar borrado (a diferencia de otras partes del documento que sí lo harían si aplicara) — se interpreta como alcance deliberado, no como omisión. Añadirlo introduciría además una pregunta no resuelta (qué hacer si la entidad ya tiene datos dependientes, dado que BAS-5 configuró esas relaciones en `Restrict`) que no aporta valor resolver ahora si no está pedido.
- **¿Un layout de Admin propio o reutilizar el público con un enlace condicional?** → Layout propio (`Areas/Admin/Pages/Shared/_Layout.cshtml`). Evita tocar el layout público (que no necesita saber nada del área Admin) y mantiene la separación por Area ya decidida en `architecture.md` punto 2.

## Referencias

- [[screens]] — sección "Área admin", tabla "Catálogo".
- [[data-model]] — entidades `Categoria`, `Club`, `Sede`, `Temporada`.
- [[archive/BAS-5/spec|BAS-5]] — incremento del que depende (entidades EF Core ya implementadas).
- [[architecture#2. Organización del código|architecture.md, punto 2]] — separación por Areas `Public`/`Admin`.
- [[architecture#7. Autenticación y autorización|architecture.md, punto 7]] — autorización del área Admin (ya vigente, sin cambios en este incremento).
