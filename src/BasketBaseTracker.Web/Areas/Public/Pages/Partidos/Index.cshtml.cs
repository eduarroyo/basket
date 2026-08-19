using BasketBaseTracker.Web.Data;
using BasketBaseTracker.Web.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

namespace BasketBaseTracker.Web.Areas.Public.Pages.Partidos;

[OutputCache(PolicyName = "Publico")]
public class IndexModel(ApplicationDbContext context) : PageModel
{
    public Partido Partido { get; set; } = null!;

    public IList<PartidoParcial> Parciales { get; set; } = [];

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        var partido = await context.Partidos
            .Include(p => p.EquipoLocal)
            .Include(p => p.EquipoVisitante)
            .Include(p => p.Sede)
            .Include(p => p.EquipoGanadorResolucion)
            .Include(p => p.Jornada).ThenInclude(j => j.Competicion).ThenInclude(c => c.Temporada)
            .Include(p => p.Jornada).ThenInclude(j => j.Competicion).ThenInclude(c => c.Categoria)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (partido is null)
        {
            return NotFound();
        }

        Partido = partido;
        Parciales = await context.PartidosParciales
            .Where(pp => pp.PartidoId == id)
            .OrderBy(pp => pp.NumeroPeriodo)
            .ToListAsync(cancellationToken);

        return Page();
    }
}
