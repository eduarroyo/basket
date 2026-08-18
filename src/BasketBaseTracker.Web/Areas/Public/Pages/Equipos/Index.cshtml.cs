using BasketBaseTracker.Web.Data;
using BasketBaseTracker.Web.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

namespace BasketBaseTracker.Web.Areas.Public.Pages.Equipos;

[OutputCache(PolicyName = "Publico")]
public class IndexModel(ApplicationDbContext context) : PageModel
{
    public Equipo Equipo { get; set; } = null!;

    public IList<FichaJugador> Plantilla { get; set; } = [];

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        var equipo = await context.Equipos
            .Include(e => e.Club)
            .Include(e => e.SedeHabitual)
            .Include(e => e.Competicion).ThenInclude(c => c.Temporada)
            .Include(e => e.Competicion).ThenInclude(c => c.Categoria)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (equipo is null)
        {
            return NotFound();
        }

        Equipo = equipo;
        Plantilla = await context.FichasJugador
            .Where(f => f.EquipoId == id)
            .OrderBy(f => f.Dorsal)
            .ToListAsync(cancellationToken);

        return Page();
    }
}
