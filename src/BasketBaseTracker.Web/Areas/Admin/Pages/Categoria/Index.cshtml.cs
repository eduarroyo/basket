using BasketBaseTracker.Web.Data;
using BasketBaseTracker.Web.Data.Entities;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BasketBaseTracker.Web.Areas.Admin.Pages.Categorias;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    public IList<Categoria> Categorias { get; set; } = [];

    public async Task OnGetAsync()
    {
        Categorias = await context.Categorias.OrderBy(c => c.Orden).ToListAsync();
    }
}
