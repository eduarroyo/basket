using BasketBaseTracker.Web.Data;
using BasketBaseTracker.Web.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BasketBaseTracker.Web.Areas.Admin.Pages.Jornadas;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    public Competicion Competicion { get; set; } = null!;

    public IList<Jornada> Jornadas { get; set; } = [];

    public async Task<IActionResult> OnGetAsync(int competicionId)
    {
        var competicion = await context.Competiciones
            .Include(c => c.Temporada)
            .Include(c => c.Categoria)
            .FirstOrDefaultAsync(c => c.Id == competicionId);
        if (competicion is null)
        {
            return NotFound();
        }

        Competicion = competicion;
        Jornadas = await context.Jornadas
            .Where(j => j.CompeticionId == competicionId)
            .OrderBy(j => j.Numero)
            .ToListAsync();

        return Page();
    }
}
