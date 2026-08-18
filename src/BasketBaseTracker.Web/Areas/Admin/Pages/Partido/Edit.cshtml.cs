using BasketBaseTracker.Web.Data;
using BasketBaseTracker.Web.Data.Entities;
using BasketBaseTracker.Web.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BasketBaseTracker.Web.Areas.Admin.Pages.Partidos;

public class EditModel(ApplicationDbContext context) : PageModel
{
    private const string MismoEquipoMensaje = "El equipo local y el visitante no pueden ser el mismo.";
    private const string EquipoRepetidoMensaje = "Alguno de los equipos ya tiene un partido programado en esta jornada.";

    public Jornada Jornada { get; set; } = null!;

    [BindProperty]
    public Partido Partido { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var partido = await CargarPartidoConJornadaAsync(id);
        if (partido is null)
        {
            return NotFound();
        }

        Partido = partido;
        Jornada = partido.Jornada;
        await CargarDesplegablesAsync(Jornada.CompeticionId);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        // Cargar y parchear en vez de Attach + EntityState.Modified: este
        // formulario no cubre Estado ni el marcador, que pertenecen a la futura
        // pantalla Resultados — marcar la entidad entera como modificada los
        // sobrescribiría con sus valores por defecto (plan.md).
        var partidoExistente = await CargarPartidoConJornadaAsync(id);
        if (partidoExistente is null)
        {
            return NotFound();
        }

        Jornada = partidoExistente.Jornada;

        if (Partido.EquipoLocalId == Partido.EquipoVisitanteId)
        {
            ModelState.AddModelError(string.Empty, MismoEquipoMensaje);
        }
        else if (await ExisteConflictoDeEquipoAsync(Jornada.Id, id))
        {
            ModelState.AddModelError(string.Empty, EquipoRepetidoMensaje);
        }

        if (!ModelState.IsValid)
        {
            await CargarDesplegablesAsync(Jornada.CompeticionId);
            return Page();
        }

        partidoExistente.EquipoLocalId = Partido.EquipoLocalId;
        partidoExistente.EquipoVisitanteId = Partido.EquipoVisitanteId;
        partidoExistente.SedeId = Partido.SedeId;
        partidoExistente.FechaHora = Partido.FechaHora;

        await context.SaveChangesAsync();

        return RedirectToPage("./Index", new { jornadaId = Jornada.Id });
    }

    private async Task<bool> ExisteConflictoDeEquipoAsync(int jornadaId, int partidoIdActual)
    {
        var partidosDeLaJornada = await context.Partidos
            .Where(p => p.JornadaId == jornadaId)
            .Select(p => new { p.Id, p.EquipoLocalId, p.EquipoVisitanteId })
            .ToListAsync();

        return PartidoReglas.EquipoYaJuegaEnJornada(
            partidosDeLaJornada.Select(p => (p.Id, p.EquipoLocalId, p.EquipoVisitanteId)),
            partidoIdActual,
            Partido.EquipoLocalId,
            Partido.EquipoVisitanteId);
    }

    private async Task<Partido?> CargarPartidoConJornadaAsync(int id) =>
        await context.Partidos
            .Include(p => p.Jornada).ThenInclude(j => j.Competicion).ThenInclude(c => c.Temporada)
            .Include(p => p.Jornada).ThenInclude(j => j.Competicion).ThenInclude(c => c.Categoria)
            .FirstOrDefaultAsync(p => p.Id == id);

    private async Task CargarDesplegablesAsync(int competicionId)
    {
        var equipos = await context.Equipos
            .Where(e => e.CompeticionId == competicionId)
            .OrderBy(e => e.Nombre)
            .ToListAsync();
        var sedes = await context.Sedes.OrderBy(s => s.Nombre).ToListAsync();

        ViewData["EquipoId"] = new SelectList(equipos, "Id", "Nombre");
        ViewData["SedeId"] = new SelectList(sedes, "Id", "Nombre");
    }
}
