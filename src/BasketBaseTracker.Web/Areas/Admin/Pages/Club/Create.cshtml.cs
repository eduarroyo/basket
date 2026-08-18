using BasketBaseTracker.Web.Data;
using BasketBaseTracker.Web.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BasketBaseTracker.Web.Areas.Admin.Pages.Clubes;

public class CreateModel(ApplicationDbContext context) : PageModel
{
    [BindProperty]
    public Club Club { get; set; } = new() { FechaAlta = DateOnly.FromDateTime(DateTime.Today) };

    public IActionResult OnGet() => Page();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        context.Clubes.Add(Club);
        await context.SaveChangesAsync();

        return RedirectToPage("./Index");
    }
}
