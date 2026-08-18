using BasketBaseTracker.Web.Data;
using BasketBaseTracker.Web.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace BasketBaseTracker.Web.Areas.Admin.Pages.Competiciones;

public class CreateModel(ApplicationDbContext context) : PageModel
{
    private const string TemporadaCategoriaDuplicadaMensaje =
        "Ya existe una competición para esa temporada y categoría.";

    [BindProperty]
    public Competicion Competicion { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        await CargarDesplegablesAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await CargarDesplegablesAsync();
            return Page();
        }

        context.Competiciones.Add(Competicion);

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (EsViolacionDeIndiceUnico(ex))
        {
            ModelState.AddModelError(string.Empty, TemporadaCategoriaDuplicadaMensaje);
            await CargarDesplegablesAsync();
            return Page();
        }

        return RedirectToPage("./Index");
    }

    private async Task CargarDesplegablesAsync()
    {
        var temporadas = await context.Temporadas
            .OrderByDescending(t => t.FechaInicio)
            .ToListAsync();
        var categorias = await context.Categorias
            .OrderBy(c => c.Orden)
            .ToListAsync();

        ViewData["TemporadaId"] = new SelectList(temporadas, "Id", "Nombre");
        ViewData["CategoriaId"] = new SelectList(categorias, "Id", "Nombre");
    }

    private static bool EsViolacionDeIndiceUnico(DbUpdateException ex) =>
        ex.InnerException is SqlException { Number: 2601 or 2627 };
}
