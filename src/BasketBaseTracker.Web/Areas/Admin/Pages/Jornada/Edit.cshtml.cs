using BasketBaseTracker.Web.Data;
using BasketBaseTracker.Web.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace BasketBaseTracker.Web.Areas.Admin.Pages.Jornadas;

public class EditModel(ApplicationDbContext context) : PageModel
{
    private const string NumeroDuplicadoMensaje = "Ya existe una jornada con ese número en esta competición.";

    public Competicion Competicion { get; set; } = null!;

    [BindProperty]
    public Jornada Jornada { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var jornada = await context.Jornadas.FindAsync(id);
        if (jornada is null)
        {
            return NotFound();
        }

        Jornada = jornada;
        await CargarCompeticionAsync(jornada.CompeticionId);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await CargarCompeticionAsync(Jornada.CompeticionId);
            return Page();
        }

        context.Attach(Jornada).State = EntityState.Modified;

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (EsViolacionDeIndiceUnico(ex))
        {
            ModelState.AddModelError(string.Empty, NumeroDuplicadoMensaje);
            await CargarCompeticionAsync(Jornada.CompeticionId);
            return Page();
        }

        return RedirectToPage("./Index", new { competicionId = Jornada.CompeticionId });
    }

    private async Task CargarCompeticionAsync(int competicionId)
    {
        Competicion = await context.Competiciones
            .Include(c => c.Temporada)
            .Include(c => c.Categoria)
            .FirstOrDefaultAsync(c => c.Id == competicionId)
            ?? throw new InvalidOperationException($"No se encontró la competición {competicionId}.");
    }

    private static bool EsViolacionDeIndiceUnico(DbUpdateException ex) =>
        ex.InnerException is SqlException { Number: 2601 or 2627 };
}
