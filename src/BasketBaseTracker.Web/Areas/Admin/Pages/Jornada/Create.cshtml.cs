using BasketBaseTracker.Web.Data;
using BasketBaseTracker.Web.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace BasketBaseTracker.Web.Areas.Admin.Pages.Jornadas;

public class CreateModel(ApplicationDbContext context) : PageModel
{
    private const string NumeroDuplicadoMensaje = "Ya existe una jornada con ese número en esta competición.";

    public Competicion Competicion { get; set; } = null!;

    [BindProperty]
    public Jornada Jornada { get; set; } = new() { CuentaParaClasificacion = true };

    public async Task<IActionResult> OnGetAsync(int competicionId)
    {
        if (!await CargarCompeticionAsync(competicionId))
        {
            return NotFound();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int competicionId)
    {
        Jornada.CompeticionId = competicionId;

        if (!ModelState.IsValid)
        {
            await CargarCompeticionAsync(competicionId);
            return Page();
        }

        context.Jornadas.Add(Jornada);

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (EsViolacionDeIndiceUnico(ex))
        {
            ModelState.AddModelError(string.Empty, NumeroDuplicadoMensaje);
            await CargarCompeticionAsync(competicionId);
            return Page();
        }

        return RedirectToPage("./Index", new { competicionId });
    }

    private async Task<bool> CargarCompeticionAsync(int competicionId)
    {
        var competicion = await context.Competiciones
            .Include(c => c.Temporada)
            .Include(c => c.Categoria)
            .FirstOrDefaultAsync(c => c.Id == competicionId);
        if (competicion is null)
        {
            return false;
        }

        Competicion = competicion;
        return true;
    }

    private static bool EsViolacionDeIndiceUnico(DbUpdateException ex) =>
        ex.InnerException is SqlException { Number: 2601 or 2627 };
}
