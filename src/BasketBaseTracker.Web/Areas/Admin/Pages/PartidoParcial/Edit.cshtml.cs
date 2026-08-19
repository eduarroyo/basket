using BasketBaseTracker.Web.Data;
using BasketBaseTracker.Web.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace BasketBaseTracker.Web.Areas.Admin.Pages.PartidosParciales;

public class EditModel(ApplicationDbContext context) : PageModel
{
    private const string PeriodoDuplicadoMensaje = "Ya existe un parcial para ese periodo en este partido.";

    public Partido Partido { get; set; } = null!;

    [BindProperty]
    public PartidoParcial PartidoParcial { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var parcial = await context.PartidosParciales.FindAsync(id);
        if (parcial is null)
        {
            return NotFound();
        }

        PartidoParcial = parcial;
        await CargarPartidoAsync(parcial.PartidoId);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await CargarPartidoAsync(PartidoParcial.PartidoId);
            return Page();
        }

        context.Attach(PartidoParcial).State = EntityState.Modified;

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (EsViolacionDeIndiceUnico(ex))
        {
            ModelState.AddModelError(string.Empty, PeriodoDuplicadoMensaje);
            await CargarPartidoAsync(PartidoParcial.PartidoId);
            return Page();
        }

        return RedirectToPage("./Index", new { partidoId = PartidoParcial.PartidoId });
    }

    private async Task CargarPartidoAsync(int partidoId)
    {
        Partido = await context.Partidos
            .Include(p => p.EquipoLocal)
            .Include(p => p.EquipoVisitante)
            .FirstOrDefaultAsync(p => p.Id == partidoId)
            ?? throw new InvalidOperationException($"No se encontró el partido {partidoId}.");
    }

    private static bool EsViolacionDeIndiceUnico(DbUpdateException ex) =>
        ex.InnerException is SqlException { Number: 2601 or 2627 };
}
