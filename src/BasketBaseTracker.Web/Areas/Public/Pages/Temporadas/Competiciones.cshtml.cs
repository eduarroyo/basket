using BasketBaseTracker.Web.Data;
using BasketBaseTracker.Web.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

namespace BasketBaseTracker.Web.Areas.Public.Pages.Temporadas;

[OutputCache(PolicyName = "Publico")]
public class CompeticionesModel(ApplicationDbContext context) : PageModel
{
    public Temporada Temporada { get; set; } = null!;

    public IList<Competicion> Competiciones { get; set; } = [];

    public Dictionary<int, List<Equipo>> EquiposPorCompeticion { get; set; } = [];

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        var temporada = await context.Temporadas.FindAsync([id], cancellationToken);
        if (temporada is null)
        {
            return NotFound();
        }

        Temporada = temporada;
        Competiciones = await context.Competiciones
            .Where(c => c.TemporadaId == id)
            .Include(c => c.Categoria)
            .OrderBy(c => c.Categoria.Orden)
            .ToListAsync(cancellationToken);

        // Competicion no tiene una colección de navegación Equipos (relación
        // unidireccional, data-model.md) — se cargan aparte y se agrupan en
        // memoria en vez de forzar una navegación inversa que ninguna otra
        // pantalla necesita (plan.md).
        var competicionIds = Competiciones.Select(c => c.Id).ToList();
        var equipos = await context.Equipos
            .Where(e => competicionIds.Contains(e.CompeticionId))
            .OrderBy(e => e.Nombre)
            .ToListAsync(cancellationToken);
        EquiposPorCompeticion = equipos
            .GroupBy(e => e.CompeticionId)
            .ToDictionary(g => g.Key, g => g.ToList());

        return Page();
    }
}
