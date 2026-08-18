---
codigo: BAS-6
estado: Planificado
tags:
  - plan
---

# BAS-6: Plan técnico

## Entidades del modelo de datos afectadas

`Temporada`, `Categoria`, `Club`, `Sede` (BAS-5) — sin cambios de esquema, solo pantallas sobre las entidades ya existentes.

## Pantallas afectadas

De `screens.md`, sección "Área admin" → "Catálogo": `Temporadas`, `Categorías`, `Clubes`, `Sedes`. Más una página de inicio del área Admin, no listada explícitamente en `screens.md` (necesaria como punto de entrada, ver `spec.md`).

## Decisiones técnicas específicas de este incremento

### Scaffolding como punto de partida, no como resultado final

Cada entidad se genera con:

```bash
dotnet aspnet-codegenerator razorpage -m <Entidad> -dc ApplicationDbContext -udl -outDir Areas/Admin/Pages/<Entidad> --referenceScriptLibraries
```

El scaffolding genera por defecto `Index`, `Create`, `Edit`, `Details` y `Delete`. Se borran `Details.cshtml(.cs)` y `Delete.cshtml(.cs)` de cada carpeta tras generar (fuera de alcance, ver `spec.md`) en vez de intentar que la herramienta no los genere — no expone esa granularidad. El resto (`Index`/`Create`/`Edit`) se revisa a mano: nombres de campos, `[Display(Name = "...")]` en español donde el nombre de la propiedad no sea ya autoexplicativo, y que la validación generada coincida con las restricciones de `Data/Configurations/*Configuration.cs` (BAS-5) — el scaffolding lee el modelo de EF Core, así que en general ya coincide, pero se revisa igual.

### Sin capa de ViewModel/DTO

Las páginas enlazan directamente contra las clases de `Data/Entities/*.cs` (`[BindProperty] public Categoria Categoria { get; set; }`, patrón por defecto del scaffolding) — no se introduce una capa de DTOs para estas cuatro pantallas simples de catálogo, coherente con la filosofía de simplicidad del proyecto (`functional.md#recursos-de-desarrollo`) y con que estas entidades no tienen ningún campo sensible que deba ocultarse del formulario.

### Autorización: sin cambios, ya cubierta por convención existente

`Program.cs` ya aplica `AuthorizeAreaFolder("Admin", "/", "Administrador")` a toda el área Admin (BAS-3), así que las páginas nuevas quedan protegidas automáticamente en cuanto se colocan bajo `Areas/Admin/Pages/`. No hace falta ningún atributo `[Authorize]` adicional por página.

### Layout propio del área Admin

`Areas/Admin/Pages/Shared/_Layout.cshtml`: copia mínima del layout público (mismo Bootstrap, mismo `site.css`) con una barra de navegación propia — enlaces a las cuatro pantallas de catálogo y "Cerrar sesión" (`POST` a `/Admin/Logout`, como ya hace `Logout.cshtml` existente). Se actualiza `Areas/Admin/Pages/_ViewStart.cshtml` para apuntar a este layout nuevo en vez de al público. `Login.cshtml` es la única página que sigue sin mostrar esta navegación (no tiene sentido antes de autenticarse) — se le fija su propio `Layout = null` o se le mantiene el layout público mínimo, a decidir al implementar según qué quede más simple.

### Redirección tras iniciar sesión

`Login.cshtml.cs` (`OnGet`/`OnPostAsync`) cambia el valor por defecto de `returnUrl` de `Url.Content("~/")` a `Url.Content("~/Admin")` — hasta ahora no había ningún sitio con sentido al que llevar al administrador tras autenticarse; con la página de inicio de este incremento, sí. `LocalRedirect` sigue respetando un `returnUrl` explícito si la sesión expiró intentando acceder a una página en concreto.

### Verificación

Test de integración por entidad (`tests/BasketBaseTracker.Tests/Integration/`), reutilizando `AppHostSqlFixture` (BAS-5) para no levantar el `AppHost` una vez por test: autenticar como el administrador de *seed*, `POST` a la página `Create` con datos válidos, comprobar que aparece en `Index`, editarlo vía `Edit`, comprobar el cambio. Más un test que confirma que una petición anónima a `/Admin/Temporada` (o cualquier otra) redirige a `/Admin/Login`.
