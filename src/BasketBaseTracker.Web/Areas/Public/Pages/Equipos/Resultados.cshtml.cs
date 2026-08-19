using BasketBaseTracker.Web.Data;
using BasketBaseTracker.Web.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

namespace BasketBaseTracker.Web.Areas.Public.Pages.Equipos;

[OutputCache(PolicyName = "Publico")]
public class ResultadosModel(ApplicationDbContext context) : PageModel
{
    public Equipo Equipo { get; set; } = null!;

    public IList<Partido> Partidos { get; set; } = [];

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        var equipo = await context.Equipos.FindAsync([id], cancellationToken);
        if (equipo is null)
        {
            return NotFound();
        }

        Equipo = equipo;
        // Un Equipo ya está scopeado a una única competición/temporada por diseño
        // (data-model.md), así que "histórico... en la temporada" no necesita
        // filtro adicional (plan.md).
        Partidos = await context.Partidos
            .Where(p => p.EquipoLocalId == id || p.EquipoVisitanteId == id)
            .Include(p => p.EquipoLocal)
            .Include(p => p.EquipoVisitante)
            .OrderByDescending(p => p.FechaHora)
            .ToListAsync(cancellationToken);

        return Page();
    }
}
