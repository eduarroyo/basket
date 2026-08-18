using BasketBaseTracker.Web.Data;
using BasketBaseTracker.Web.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BasketBaseTracker.Web.Areas.Admin.Pages.Clubes;

public class EditModel(ApplicationDbContext context) : PageModel
{
    [BindProperty]
    public Club Club { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var club = await context.Clubes.FindAsync(id);
        if (club is null)
        {
            return NotFound();
        }

        Club = club;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        context.Attach(Club).State = EntityState.Modified;
        await context.SaveChangesAsync();

        return RedirectToPage("./Index");
    }
}
