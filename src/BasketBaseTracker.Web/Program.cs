var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

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
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.MapDefaultEndpoints();

app.Run();
