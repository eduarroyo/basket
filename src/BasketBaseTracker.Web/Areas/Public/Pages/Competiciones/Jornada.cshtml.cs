using BasketBaseTracker.Web.Data;
using BasketBaseTracker.Web.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

namespace BasketBaseTracker.Web.Areas.Public.Pages.Competiciones;

[OutputCache(PolicyName = "Publico")]
public class JornadaModel(ApplicationDbContext context) : PageModel
{
    public Jornada Jornada { get; set; } = null!;

    public IList<Partido> Partidos { get; set; } = [];

    // Direcciona por (CompeticionId, Numero), no por el Id interno de Jornada —
    // Numero solo es único dentro de una competición (restricción de BAS-9), y da
    // una URL más significativa/estable (screens.md, plan.md).
    public async Task<IActionResult> OnGetAsync(int id, int n, CancellationToken cancellationToken)
    {
        var jornada = await context.Jornadas
            .Include(j => j.Competicion).ThenInclude(c => c.Temporada)
            .Include(j => j.Competicion).ThenInclude(c => c.Categoria)
            .FirstOrDefaultAsync(j => j.CompeticionId == id && j.Numero == n, cancellationToken);
        if (jornada is null)
        {
            return NotFound();
        }

        Jornada = jornada;
        Partidos = await context.Partidos
            .Where(p => p.JornadaId == jornada.Id)
            .Include(p => p.EquipoLocal)
            .Include(p => p.EquipoVisitante)
            .OrderBy(p => p.FechaHora)
            .ToListAsync(cancellationToken);

        return Page();
    }
}
