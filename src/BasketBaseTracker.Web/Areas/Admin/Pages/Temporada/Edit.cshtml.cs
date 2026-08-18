using BasketBaseTracker.Web.Data;
using BasketBaseTracker.Web.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BasketBaseTracker.Web.Areas.Admin.Pages.Temporadas;

public class EditModel(ApplicationDbContext context) : PageModel
{
    [BindProperty]
    public Temporada Temporada { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var temporada = await context.Temporadas.FindAsync(id);
        if (temporada is null)
        {
            return NotFound();
        }

        Temporada = temporada;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        context.Attach(Temporada).State = EntityState.Modified;
        await context.SaveChangesAsync();

        return RedirectToPage("./Index");
    }
}
