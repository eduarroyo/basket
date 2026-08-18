using BasketBaseTracker.Web.Data;
using BasketBaseTracker.Web.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BasketBaseTracker.Web.Areas.Admin.Pages.FichasJugador;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    public Equipo Equipo { get; set; } = null!;

    public IList<FichaJugador> Fichas { get; set; } = [];

    public async Task<IActionResult> OnGetAsync(int equipoId)
    {
        var equipo = await context.Equipos.FindAsync(equipoId);
        if (equipo is null)
        {
            return NotFound();
        }

        Equipo = equipo;
        Fichas = await context.FichasJugador
            .Where(f => f.EquipoId == equipoId)
            .OrderBy(f => f.Dorsal)
            .ToListAsync();

        return Page();
    }
}
