using BasketBaseTracker.Web.Data;
using BasketBaseTracker.Web.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BasketBaseTracker.Web.Areas.Admin.Pages.Categorias;

public class CreateModel(ApplicationDbContext context) : PageModel
{
    [BindProperty]
    public Categoria Categoria { get; set; } = new();

    public IActionResult OnGet() => Page();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        context.Categorias.Add(Categoria);
        await context.SaveChangesAsync();

        return RedirectToPage("./Index");
    }
}
