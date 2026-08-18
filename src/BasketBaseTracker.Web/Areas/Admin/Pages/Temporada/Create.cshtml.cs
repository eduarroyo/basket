using BasketBaseTracker.Web.Data;
using BasketBaseTracker.Web.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BasketBaseTracker.Web.Areas.Admin.Pages.Temporadas;

public class CreateModel(ApplicationDbContext context) : PageModel
{
    [BindProperty]
    public Temporada Temporada { get; set; } = new();

    public IActionResult OnGet() => Page();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        context.Temporadas.Add(Temporada);
        await context.SaveChangesAsync();

        return RedirectToPage("./Index");
    }
}
