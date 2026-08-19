using BasketBaseTracker.Web.Data;
using BasketBaseTracker.Web.Data.Entities;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BasketBaseTracker.Web.Areas.Admin.Pages.Competiciones;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    public IList<Competicion> Competiciones { get; set; } = [];

    public async Task OnGetAsync()
    {
        Competiciones = await context.Competiciones
            .Include(c => c.Temporada)
            .Include(c => c.Categoria)
            .OrderByDescending(c => c.Temporada.FechaInicio)
            .ThenBy(c => c.Categoria.Orden)
            .ToListAsync();
    }
}
