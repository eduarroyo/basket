using BasketBaseTracker.Web.Data;
using BasketBaseTracker.Web.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BasketBaseTracker.Web.Areas.Admin.Pages.Partidos;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    public Jornada Jornada { get; set; } = null!;

    public IList<Partido> Partidos { get; set; } = [];

    public async Task<IActionResult> OnGetAsync(int jornadaId)
    {
        var jornada = await context.Jornadas
            .Include(j => j.Competicion).ThenInclude(c => c.Temporada)
            .Include(j => j.Competicion).ThenInclude(c => c.Categoria)
            .FirstOrDefaultAsync(j => j.Id == jornadaId);
        if (jornada is null)
        {
            return NotFound();
        }

        Jornada = jornada;
        Partidos = await context.Partidos
            .Where(p => p.JornadaId == jornadaId)
            .Include(p => p.EquipoLocal)
            .Include(p => p.EquipoVisitante)
            .Include(p => p.Sede)
            .OrderBy(p => p.FechaHora)
            .ToListAsync();

        return Page();
    }
}
