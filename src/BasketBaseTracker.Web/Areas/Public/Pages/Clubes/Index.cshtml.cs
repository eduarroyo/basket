using BasketBaseTracker.Web.Data;
using BasketBaseTracker.Web.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

namespace BasketBaseTracker.Web.Areas.Public.Pages.Clubes;

[OutputCache(PolicyName = "Publico")]
public class IndexModel(ApplicationDbContext context) : PageModel
{
    public Club Club { get; set; } = null!;

    public IList<Equipo> EquiposTemporadaActual { get; set; } = [];

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        var club = await context.Clubes.FindAsync([id], cancellationToken);
        if (club is null)
        {
            return NotFound();
        }

        Club = club;
        // Lista vacía si no hay ninguna temporada EnCurso — no hace falta un caso
        // especial adicional (plan.md).
        EquiposTemporadaActual = await context.Equipos
            .Include(e => e.Competicion).ThenInclude(c => c.Temporada)
            .Include(e => e.Competicion).ThenInclude(c => c.Categoria)
            .Where(e => e.ClubId == id && e.Competicion.Temporada.Estado == TemporadaEstado.EnCurso)
            .OrderBy(e => e.Nombre)
            .ToListAsync(cancellationToken);

        return Page();
    }
}
