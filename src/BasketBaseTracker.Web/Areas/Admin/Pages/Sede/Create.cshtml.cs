using BasketBaseTracker.Web.Data;
using BasketBaseTracker.Web.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BasketBaseTracker.Web.Areas.Admin.Pages.Sedes;

public class CreateModel(ApplicationDbContext context) : PageModel
{
    [BindProperty]
    public Sede Sede { get; set; } = new();

    public IActionResult OnGet() => Page();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        context.Sedes.Add(Sede);
        await context.SaveChangesAsync();

        return RedirectToPage("./Index");
    }
}
