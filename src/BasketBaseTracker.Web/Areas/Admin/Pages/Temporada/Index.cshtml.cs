using BasketBaseTracker.Web.Data;
using BasketBaseTracker.Web.Data.Entities;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BasketBaseTracker.Web.Areas.Admin.Pages.Temporadas;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    public IList<Temporada> Temporadas { get; set; } = [];

    public async Task OnGetAsync()
    {
        Temporadas = await context.Temporadas.OrderByDescending(t => t.FechaInicio).ToListAsync();
    }
}
