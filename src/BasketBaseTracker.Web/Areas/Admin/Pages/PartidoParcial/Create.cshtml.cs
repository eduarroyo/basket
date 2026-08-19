using BasketBaseTracker.Web.Data;
using BasketBaseTracker.Web.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace BasketBaseTracker.Web.Areas.Admin.Pages.PartidosParciales;

public class CreateModel(ApplicationDbContext context) : PageModel
{
    private const string PeriodoDuplicadoMensaje = "Ya existe un parcial para ese periodo en este partido.";

    public Partido Partido { get; set; } = null!;

    [BindProperty]
    public PartidoParcial PartidoParcial { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int partidoId)
    {
        if (!await CargarPartidoAsync(partidoId))
        {
            return NotFound();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int partidoId)
    {
        PartidoParcial.PartidoId = partidoId;

        if (!ModelState.IsValid)
        {
            await CargarPartidoAsync(partidoId);
            return Page();
        }

        context.PartidosParciales.Add(PartidoParcial);

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (EsViolacionDeIndiceUnico(ex))
        {
            ModelState.AddModelError(string.Empty, PeriodoDuplicadoMensaje);
            await CargarPartidoAsync(partidoId);
            return Page();
        }

        return RedirectToPage("./Index", new { partidoId });
    }

    private async Task<bool> CargarPartidoAsync(int partidoId)
    {
        var partido = await context.Partidos
            .Include(p => p.EquipoLocal)
            .Include(p => p.EquipoVisitante)
            .FirstOrDefaultAsync(p => p.Id == partidoId);
        if (partido is null)
        {
            return false;
        }

        Partido = partido;
        return true;
    }

    private static bool EsViolacionDeIndiceUnico(DbUpdateException ex) =>
        ex.InnerException is SqlException { Number: 2601 or 2627 };
}
