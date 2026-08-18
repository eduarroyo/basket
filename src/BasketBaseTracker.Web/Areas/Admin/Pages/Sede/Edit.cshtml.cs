using BasketBaseTracker.Web.Data;
using BasketBaseTracker.Web.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BasketBaseTracker.Web.Areas.Admin.Pages.Sedes;

public class EditModel(ApplicationDbContext context) : PageModel
{
    [BindProperty]
    public Sede Sede { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var sede = await context.Sedes.FindAsync(id);
        if (sede is null)
        {
            return NotFound();
        }

        Sede = sede;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        context.Attach(Sede).State = EntityState.Modified;
        await context.SaveChangesAsync();

        return RedirectToPage("./Index");
    }
}
