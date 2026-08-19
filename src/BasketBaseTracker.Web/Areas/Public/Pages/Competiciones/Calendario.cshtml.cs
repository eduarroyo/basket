using BasketBaseTracker.Web.Data;
using BasketBaseTracker.Web.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

namespace BasketBaseTracker.Web.Areas.Public.Pages.Competiciones;

[OutputCache(PolicyName = "Publico")]
public class CalendarioModel(ApplicationDbContext context) : PageModel
{
    public Competicion Competicion { get; set; } = null!;

    public IList<Jornada> Jornadas { get; set; } = [];

    public Dictionary<int, List<Partido>> PartidosPorJornada { get; set; } = [];

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        var competicion = await context.Competiciones
            .Include(c => c.Temporada)
            .Include(c => c.Categoria)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (competicion is null)
        {
            return NotFound();
        }

        Competicion = competicion;
        Jornadas = await context.Jornadas
            .Where(j => j.CompeticionId == id)
            .OrderBy(j => j.Numero)
            .ToListAsync(cancellationToken);

        var jornadaIds = Jornadas.Select(j => j.Id).ToList();
        var partidos = await context.Partidos
            .Where(p => jornadaIds.Contains(p.JornadaId))
            .Include(p => p.EquipoLocal)
            .Include(p => p.EquipoVisitante)
            .Include(p => p.Sede)
            .OrderBy(p => p.FechaHora)
            .ToListAsync(cancellationToken);
        PartidosPorJornada = partidos
            .GroupBy(p => p.JornadaId)
            .ToDictionary(g => g.Key, g => g.ToList());

        return Page();
    }
}
