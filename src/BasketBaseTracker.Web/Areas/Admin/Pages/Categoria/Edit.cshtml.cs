using BasketBaseTracker.Web.Data;
using BasketBaseTracker.Web.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BasketBaseTracker.Web.Areas.Admin.Pages.Categorias;

public class EditModel(ApplicationDbContext context) : PageModel
{
    [BindProperty]
    public Categoria Categoria { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var categoria = await context.Categorias.FindAsync(id);
        if (categoria is null)
        {
            return NotFound();
        }

        Categoria = categoria;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        context.Attach(Categoria).State = EntityState.Modified;
        await context.SaveChangesAsync();

        return RedirectToPage("./Index");
    }
}
