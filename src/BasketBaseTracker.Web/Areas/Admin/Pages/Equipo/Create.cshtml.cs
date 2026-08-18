using BasketBaseTracker.Web.Data;
using BasketBaseTracker.Web.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BasketBaseTracker.Web.Areas.Admin.Pages.Equipos;

public class CreateModel(ApplicationDbContext context) : PageModel
{
    [BindProperty]
    public Equipo Equipo { get; set; } = new() { Estado = EquipoEstado.Activo };

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

        context.Equipos.Add(Equipo);
        await context.SaveChangesAsync();

        return RedirectToPage("./Index");
    }

    private async Task CargarDesplegablesAsync()
    {
        // Competicion no tiene un nombre propio: se combina Temporada + Categoría
        // como texto de cada opción (BAS-7, plan.md).
        var competiciones = await context.Competiciones
            .Include(c => c.Temporada)
            .Include(c => c.Categoria)
            .OrderByDescending(c => c.Temporada.FechaInicio)
            .Select(c => new { c.Id, Texto = c.Temporada.Nombre + " — " + c.Categoria.Nombre })
            .ToListAsync();
        var clubes = await context.Clubes.OrderBy(c => c.Nombre).ToListAsync();
        var sedes = await context.Sedes.OrderBy(s => s.Nombre).ToListAsync();

        ViewData["CompeticionId"] = new SelectList(competiciones, "Id", "Texto");
        ViewData["ClubId"] = new SelectList(clubes, "Id", "Nombre");
        ViewData["SedeHabitualId"] = new SelectList(sedes, "Id", "Nombre");
    }
}
