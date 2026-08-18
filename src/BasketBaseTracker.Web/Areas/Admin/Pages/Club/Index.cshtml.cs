using BasketBaseTracker.Web.Data;
using BasketBaseTracker.Web.Data.Entities;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BasketBaseTracker.Web.Areas.Admin.Pages.Clubes;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    public IList<Club> Clubes { get; set; } = [];

    public async Task OnGetAsync()
    {
        Clubes = await context.Clubes.OrderBy(c => c.Nombre).ToListAsync();
    }
}
