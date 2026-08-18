using BasketBaseTracker.Web.Data;
using BasketBaseTracker.Web.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace BasketBaseTracker.Web.Areas.Admin.Pages.FichasJugador;

public class EditModel(ApplicationDbContext context) : PageModel
{
    private const string DorsalDuplicadoMensaje = "Ya existe un jugador con ese dorsal en este equipo.";

    public Equipo Equipo { get; set; } = null!;

    [BindProperty]
    public FichaJugador FichaJugador { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var ficha = await context.FichasJugador.FindAsync(id);
        if (ficha is null)
        {
            return NotFound();
        }

        FichaJugador = ficha;
        await CargarEquipoAsync(ficha.EquipoId);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await CargarEquipoAsync(FichaJugador.EquipoId);
            return Page();
        }

        context.Attach(FichaJugador).State = EntityState.Modified;

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (EsViolacionDeIndiceUnico(ex))
        {
            ModelState.AddModelError(string.Empty, DorsalDuplicadoMensaje);
            await CargarEquipoAsync(FichaJugador.EquipoId);
            return Page();
        }

        return RedirectToPage("./Index", new { equipoId = FichaJugador.EquipoId });
    }

    private async Task CargarEquipoAsync(int equipoId)
    {
        Equipo = await context.Equipos.FindAsync(equipoId)
            ?? throw new InvalidOperationException($"No se encontró el equipo {equipoId}.");
    }

    private static bool EsViolacionDeIndiceUnico(DbUpdateException ex) =>
        ex.InnerException is SqlException { Number: 2601 or 2627 };
}
