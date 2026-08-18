using BasketBaseTracker.Web.Data;
using BasketBaseTracker.Web.Data.Entities;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BasketBaseTracker.Web.Areas.Admin.Pages.Sedes;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    public IList<Sede> Sedes { get; set; } = [];

    public async Task OnGetAsync()
    {
        Sedes = await context.Sedes.OrderBy(s => s.Nombre).ToListAsync();
    }
}
