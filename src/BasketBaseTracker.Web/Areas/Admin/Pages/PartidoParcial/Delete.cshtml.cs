using BasketBaseTracker.Web.Data;
using BasketBaseTracker.Web.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BasketBaseTracker.Web.Areas.Admin.Pages.PartidosParciales;

public class DeleteModel(ApplicationDbContext context) : PageModel
{
    public Partido Partido { get; set; } = null!;

    [BindProperty]
    public PartidoParcial PartidoParcial { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var parcial = await context.PartidosParciales.FindAsync(id);
        if (parcial is null)
        {
            return NotFound();
        }

        PartidoParcial = parcial;
        Partido = await context.Partidos
            .Include(p => p.EquipoLocal)
            .Include(p => p.EquipoVisitante)
            .FirstOrDefaultAsync(p => p.Id == parcial.PartidoId)
            ?? throw new InvalidOperationException($"No se encontró el partido {parcial.PartidoId}.");
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var parcial = await context.PartidosParciales.FindAsync(PartidoParcial.Id);
        if (parcial is null)
        {
            return NotFound();
        }

        var partidoId = parcial.PartidoId;
        context.PartidosParciales.Remove(parcial);
        await context.SaveChangesAsync();

        return RedirectToPage("./Index", new { partidoId });
    }
}
