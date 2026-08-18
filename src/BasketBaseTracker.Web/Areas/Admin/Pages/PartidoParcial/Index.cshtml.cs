using BasketBaseTracker.Web.Data;
using BasketBaseTracker.Web.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BasketBaseTracker.Web.Areas.Admin.Pages.PartidosParciales;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    public Partido Partido { get; set; } = null!;

    public IList<PartidoParcial> Parciales { get; set; } = [];

    public async Task<IActionResult> OnGetAsync(int partidoId)
    {
        var partido = await context.Partidos
            .Include(p => p.EquipoLocal)
            .Include(p => p.EquipoVisitante)
            .FirstOrDefaultAsync(p => p.Id == partidoId);
        if (partido is null)
        {
            return NotFound();
        }

        Partido = partido;
        Parciales = await context.PartidosParciales
            .Where(p => p.PartidoId == partidoId)
            .OrderBy(p => p.NumeroPeriodo)
            .ToListAsync();

        return Page();
    }
}
