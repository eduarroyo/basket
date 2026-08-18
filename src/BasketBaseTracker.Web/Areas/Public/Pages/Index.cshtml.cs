using BasketBaseTracker.Web.Data;
using BasketBaseTracker.Web.Data.Entities;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

namespace BasketBaseTracker.Web.Areas.Public.Pages;

[OutputCache(PolicyName = "Publico")]
public class IndexModel(ApplicationDbContext context) : PageModel
{
    public IList<Temporada> Temporadas { get; set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Temporadas = await context.Temporadas
            .OrderByDescending(t => t.FechaInicio)
            .ToListAsync(cancellationToken);
    }
}
