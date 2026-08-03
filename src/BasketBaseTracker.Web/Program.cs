using BasketBaseTracker.Web.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddSqlServerDbContext<ApplicationDbContext>("basketbasetracker");

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
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Administrador", policy => policy.RequireRole("Administrador"));
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

    // El Area "Admin" exige el rol "Administrador" en todas sus páginas, salvo Login
    // (si no, nadie podría llegar a autenticarse) y Logout (para poder mostrar la
    // confirmación de cierre de sesión ya sin sesión activa).
    options.Conventions.AuthorizeAreaFolder("Admin", "/", "Administrador");
    options.Conventions.AllowAnonymousToAreaPage("Admin", "/Login");
    options.Conventions.AllowAnonymousToAreaPage("Admin", "/Logout");
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

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.MapDefaultEndpoints();

app.Run();
