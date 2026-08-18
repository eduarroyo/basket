using BasketBaseTracker.Web.Data;
using BasketBaseTracker.Web.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;

namespace BasketBaseTracker.Web.Areas.Public.Pages.Sedes;

[OutputCache(PolicyName = "Publico")]
public class IndexModel(ApplicationDbContext context) : PageModel
{
    public Sede Sede { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        var sede = await context.Sedes.FindAsync([id], cancellationToken);
        if (sede is null)
        {
            return NotFound();
        }

        Sede = sede;
        return Page();
    }
}
