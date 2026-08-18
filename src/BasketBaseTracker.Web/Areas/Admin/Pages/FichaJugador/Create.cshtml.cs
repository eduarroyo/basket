using BasketBaseTracker.Web.Data;
using BasketBaseTracker.Web.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace BasketBaseTracker.Web.Areas.Admin.Pages.FichasJugador;

public class CreateModel(ApplicationDbContext context) : PageModel
{
    private const string DorsalDuplicadoMensaje = "Ya existe un jugador con ese dorsal en este equipo.";

    public Equipo Equipo { get; set; } = null!;

    [BindProperty]
    public FichaJugador FichaJugador { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int equipoId)
    {
        var equipo = await context.Equipos.FindAsync(equipoId);
        if (equipo is null)
        {
            return NotFound();
        }

        Equipo = equipo;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int equipoId)
    {
        FichaJugador.EquipoId = equipoId;

        if (!ModelState.IsValid)
        {
            await CargarEquipoAsync(equipoId);
            return Page();
        }

        context.FichasJugador.Add(FichaJugador);

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (EsViolacionDeIndiceUnico(ex))
        {
            ModelState.AddModelError(string.Empty, DorsalDuplicadoMensaje);
            await CargarEquipoAsync(equipoId);
            return Page();
        }

        return RedirectToPage("./Index", new { equipoId });
    }

    private async Task CargarEquipoAsync(int equipoId)
    {
        Equipo = await context.Equipos.FindAsync(equipoId)
            ?? throw new InvalidOperationException($"No se encontró el equipo {equipoId}.");
    }

    private static bool EsViolacionDeIndiceUnico(DbUpdateException ex) =>
        ex.InnerException is SqlException { Number: 2601 or 2627 };
}
