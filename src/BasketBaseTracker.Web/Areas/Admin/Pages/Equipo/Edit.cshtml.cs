using BasketBaseTracker.Web.Data;
using BasketBaseTracker.Web.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BasketBaseTracker.Web.Areas.Admin.Pages.Equipos;

public class EditModel(ApplicationDbContext context) : PageModel
{
    private const string ConflictoDeConcurrenciaMensaje =
        "Este equipo se ha modificado en otro sitio mientras tanto. Recarga la página e inténtalo de nuevo.";

    [BindProperty]
    public Equipo Equipo { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var equipo = await context.Equipos.FindAsync(id);
        if (equipo is null)
        {
            return NotFound();
        }

        Equipo = equipo;
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

        context.Attach(Equipo).State = EntityState.Modified;

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            ModelState.AddModelError(string.Empty, ConflictoDeConcurrenciaMensaje);
            await CargarDesplegablesAsync();
            return Page();
        }

        return RedirectToPage("./Index");
    }

    private async Task CargarDesplegablesAsync()
    {
        var competiciones = await context.Competiciones
            .Include(c => c.Temporada)
            .Include(c => c.Categoria)
            .OrderByDescending(c => c.Temporada.FechaInicio)
            .Select(c => new { c.Id, Texto = c.Temporada.Nombre + " — " + c.Categoria.Nombre })
            .ToListAsync();
        var clubes = await context.Clubes.OrderBy(c => c.Nombre).ToListAsync();
        var sedes = await context.Sedes.OrderBy(s => s.Nombre).ToListAsync();

        ViewData["CompeticionId"] = new SelectList(competiciones, "Id", "Texto", Equipo.CompeticionId);
        ViewData["ClubId"] = new SelectList(clubes, "Id", "Nombre", Equipo.ClubId);
        ViewData["SedeHabitualId"] = new SelectList(sedes, "Id", "Nombre", Equipo.SedeHabitualId);
    }
}
