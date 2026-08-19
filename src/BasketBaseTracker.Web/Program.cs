using System.Text;
using BasketBaseTracker.Web.Data;
using BasketBaseTracker.Web.Data.Entities;
using BasketBaseTracker.Web.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Solo fuera de Development: en producción, Web referencia el recurso Key Vault
// "kv" (AppHost.cs, solo en modo publish) y sus secretos (Seed--AdminEmail,
// Seed--AdminPassword) se cargan aquí en IConfiguration — en local se sigue usando
// dotnet user-secrets tal cual (spec.md de BAS-3).
if (!builder.Environment.IsDevelopment())
{
    builder.Configuration.AddAzureKeyVaultSecrets("kv");
}

builder.AddServiceDefaults();

// Reintentos ante fallos transitorios de Azure SQL (tabla de configuración en
// README.md). AddSqlServerDbContext agrupa los DbContext en un pool por
// rendimiento — EF Core prohíbe sobreescribir OnConfiguring cuando el pooling
// está activo (InvalidOperationException en el primer uso, detectado en
// producción: BAS-3, spec.md), así que los reintentos se configuran aquí, una
// sola vez al arrancar, vía configureDbContextOptions — ya no son ajustables en
// caliente sin reiniciar la app (ni siquiera en local), a diferencia de lo que
// se documentó originalmente.
builder.Services.Configure<SqlResilienceOptions>(
    builder.Configuration.GetSection(SqlResilienceOptions.SectionName));

var sqlResilience = builder.Configuration
    .GetSection(SqlResilienceOptions.SectionName)
    .Get<SqlResilienceOptions>() ?? new SqlResilienceOptions();

builder.AddSqlServerDbContext<ApplicationDbContext>(
    "basketbasetracker",
    configureDbContextOptions: options => options.UseSqlServer(sqlServerOptions =>
        sqlServerOptions.EnableRetryOnFailure(
            sqlResilience.MaxRetryCount,
            TimeSpan.FromSeconds(sqlResilience.MaxRetryDelaySeconds),
            errorNumbersToAdd: null)));

// Backup automático antes de una importación (BAS-16) — emulador Azurite en
// local/tests, Azure Storage real al publicar (AppHost.cs).
builder.AddAzureBlobServiceClient("blobs");

// Identity: autenticación por cookies, con roles (rol único "Administrador" en v1 —
// architecture.md punto 7). Sin autorregistro ni confirmación por email, así que se
// desactiva ese flujo; contraseña más estricta que el valor por defecto de Identity.
builder.Services
    .AddIdentity<IdentityUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequiredLength = 12;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Admin/Login";
    options.LogoutPath = "/Admin/Logout";

    // Sin esto, un usuario autenticado sin el rol requerido caía en el
    // AccessDeniedPath por defecto de Identity ("/Account/AccessDenied", que no
    // existe en esta app) y veía un 404 en vez de un 403 — nunca se notó hasta
    // BAS-16, que introduce el primer caso real de "autenticado pero sin el rol
    // correcto" (antes solo había un único rol, válido para todo el área Admin).
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});

builder.Services.AddAuthorization(options =>
{
    // "Administrador" = administrador del sistema (import/export, BAS-16);
    // "GestorCompeticion" = gestión de la competición (resto del área Admin) — antes
    // un único rol "Administrador", dividido en BAS-16 (architecture.md punto 7).
    options.AddPolicy("Administrador", policy => policy.RequireRole("Administrador"));
    options.AddPolicy("GestorCompeticion", policy => policy.RequireRole("GestorCompeticion"));
    // RequireRole con varios roles es "o" (al menos uno) — para el panel de
    // administración (Admin/Index), la página compartida de aterrizaje tras el
    // login para ambos tipos de cuenta.
    options.AddPolicy("GestorCompeticionOAdministrador", policy => policy.RequireRole("GestorCompeticion", "Administrador"));
});

builder.Services.AddScoped<ClasificacionService>();

// Output Caching de las páginas públicas (architecture.md punto 5) — TTL de 4
// minutos, ligeramente por debajo del límite de 5 de consistencia eventual de
// functional.md, dejando margen para la propagación hasta el edge de Cloudflare
// (BAS-4, todavía sin configurar). Sin política base global: solo se cachea el
// endpoint que la use explícitamente vía [OutputCache(PolicyName = "Publico")],
// así que el área Admin queda sin caché por construcción, sin exclusión aparte.
builder.Services.AddOutputCache(options =>
{
    options.AddPolicy("Publico", policy => policy.Expire(TimeSpan.FromMinutes(4)));
});

// Add services to the container.
builder.Services.AddRazorPages(options =>
{
    // El Area "Public" no lleva prefijo de ruta: sus páginas deben resolver en la
    // misma URL que si no hubiera Areas (p. ej. Index -> "/", no "/Public"), tal
    // como define docs/screens.md. El Area "Admin" sí conserva su prefijo por defecto.
    options.Conventions.AddAreaFolderRouteModelConvention("Public", "/", model =>
    {
        foreach (var selector in model.Selectors)
        {
            var template = selector.AttributeRouteModel?.Template;
            if (string.IsNullOrEmpty(template))
            {
                continue;
            }

            if (template.Equals("Public", StringComparison.OrdinalIgnoreCase))
            {
                selector.AttributeRouteModel!.Template = string.Empty;
            }
            else if (template.StartsWith("Public/", StringComparison.OrdinalIgnoreCase))
            {
                selector.AttributeRouteModel!.Template = template["Public/".Length..];
            }
        }
    });

    // El Area "Admin" exige el rol "GestorCompeticion" en todas sus páginas, salvo
    // Login (si no, nadie podría llegar a autenticarse), Logout (para poder mostrar
    // la confirmación de cierre de sesión ya sin sesión activa), el panel de
    // aterrizaje (/Index, compartido por ambos tipos de cuenta tras el login) e
    // ImportExport, que exige en su lugar el rol "Administrador" (sistema) — BAS-16.
    // No se puede usar AuthorizeAreaFolder + AuthorizeAreaPage para estas páginas
    // porque los filtros de autorización se acumulan (AND, no reemplazo): exigiría
    // varios roles a la vez. Se excluyen de la convención de carpeta con un filtro
    // propio y se les da su propia política aparte.
    options.Conventions.AddAreaFolderApplicationModelConvention("Admin", "/", model =>
    {
        if (!model.RelativePath.Contains("/ImportExport/", StringComparison.OrdinalIgnoreCase)
            && !model.RelativePath.EndsWith("/Pages/Index.cshtml", StringComparison.OrdinalIgnoreCase))
        {
            model.Filters.Add(new AuthorizeFilter("GestorCompeticion"));
        }
    });
    options.Conventions.AllowAnonymousToAreaPage("Admin", "/Login");
    options.Conventions.AllowAnonymousToAreaPage("Admin", "/Logout");
    options.Conventions.AuthorizeAreaPage("Admin", "/ImportExport/Index", "Administrador");
    options.Conventions.AuthorizeAreaPage("Admin", "/Index", "GestorCompeticionOAdministrador");
});

var app = builder.Build();

// Solo en desarrollo: en el resto de entornos las migraciones se aplican como paso
// explícito del pipeline (architecture.md punto 13), no al arrancar cada instancia
// (evita que varias réplicas intenten migrar a la vez).
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.MigrateAsync();
}

await IdentitySeeder.SeedAdministradorAsync(app);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseOutputCache();

// Suscripción iCal (BAS-14) — Minimal API en vez de Razor Pages porque no
// producen HTML. Reutilizan la misma política de caché "Publico" que el resto
// del área pública (architecture.md, punto 5).
app.MapGet("/competiciones/{id:int}/calendario.ics", async (int id, ApplicationDbContext context, CancellationToken cancellationToken) =>
{
    var existeCompeticion = await context.Competiciones.AnyAsync(c => c.Id == id, cancellationToken);
    if (!existeCompeticion)
    {
        return Results.NotFound();
    }

    var partidos = await context.Partidos
        .Where(p => p.Jornada.CompeticionId == id && p.FechaHora != null && p.Estado != PartidoEstado.Cancelado)
        .Select(p => new PartidoIcs(
            p.Id, p.EquipoLocal.Nombre, p.EquipoVisitante.Nombre, p.FechaHora!.Value,
            p.Sede == null ? null : p.Sede.Nombre, p.Sede == null ? null : p.Sede.Municipio))
        .ToListAsync(cancellationToken);

    return Results.Text(IcsFeedBuilder.Construir(partidos), "text/calendar", Encoding.UTF8);
}).CacheOutput("Publico");

app.MapGet("/equipos/{id:int}/calendario.ics", async (int id, ApplicationDbContext context, CancellationToken cancellationToken) =>
{
    var existeEquipo = await context.Equipos.AnyAsync(e => e.Id == id, cancellationToken);
    if (!existeEquipo)
    {
        return Results.NotFound();
    }

    var partidos = await context.Partidos
        .Where(p => (p.EquipoLocalId == id || p.EquipoVisitanteId == id) && p.FechaHora != null && p.Estado != PartidoEstado.Cancelado)
        .Select(p => new PartidoIcs(
            p.Id, p.EquipoLocal.Nombre, p.EquipoVisitante.Nombre, p.FechaHora!.Value,
            p.Sede == null ? null : p.Sede.Nombre, p.Sede == null ? null : p.Sede.Municipio))
        .ToListAsync(cancellationToken);

    return Results.Text(IcsFeedBuilder.Construir(partidos), "text/calendar", Encoding.UTF8);
}).CacheOutput("Publico");

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.MapDefaultEndpoints();

app.Run();
