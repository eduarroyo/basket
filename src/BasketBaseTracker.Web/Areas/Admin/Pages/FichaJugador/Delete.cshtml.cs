using BasketBaseTracker.Web.Data;
using BasketBaseTracker.Web.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BasketBaseTracker.Web.Areas.Admin.Pages.FichasJugador;

public class DeleteModel(ApplicationDbContext context) : PageModel
{
    public Equipo Equipo { get; set; } = null!;

    [BindProperty]
    public FichaJugador FichaJugador { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var ficha = await context.FichasJugador.FindAsync(id);
        if (ficha is null)
        {
            return NotFound();
        }

        FichaJugador = ficha;
        Equipo = await context.Equipos.FindAsync(ficha.EquipoId)
            ?? throw new InvalidOperationException($"No se encontró el equipo {ficha.EquipoId}.");
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var ficha = await context.FichasJugador.FindAsync(FichaJugador.Id);
        if (ficha is null)
        {
            return NotFound();
        }

        var equipoId = ficha.EquipoId;
        context.FichasJugador.Remove(ficha);
        await context.SaveChangesAsync();

        return RedirectToPage("./Index", new { equipoId });
    }
}
