using BasketBaseTracker.Web.Data;
using BasketBaseTracker.Web.Data.Entities;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BasketBaseTracker.Web.Areas.Admin.Pages.Equipos;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    public IList<Equipo> Equipos { get; set; } = [];

    public async Task OnGetAsync()
    {
        Equipos = await context.Equipos
            .Include(e => e.Competicion).ThenInclude(c => c.Temporada)
            .Include(e => e.Competicion).ThenInclude(c => c.Categoria)
            .Include(e => e.Club)
            .Include(e => e.SedeHabitual)
            .OrderBy(e => e.Nombre)
            .ToListAsync();
    }
}
