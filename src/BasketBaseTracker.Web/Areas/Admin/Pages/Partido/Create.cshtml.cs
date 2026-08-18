using BasketBaseTracker.Web.Data;
using BasketBaseTracker.Web.Data.Entities;
using BasketBaseTracker.Web.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BasketBaseTracker.Web.Areas.Admin.Pages.Partidos;

public class CreateModel(ApplicationDbContext context) : PageModel
{
    private const string MismoEquipoMensaje = "El equipo local y el visitante no pueden ser el mismo.";
    private const string EquipoRepetidoMensaje = "Alguno de los equipos ya tiene un partido programado en esta jornada.";

    public Jornada Jornada { get; set; } = null!;

    [BindProperty]
    public Partido Partido { get; set; } = new() { Estado = PartidoEstado.Programado };

    public async Task<IActionResult> OnGetAsync(int jornadaId)
    {
        if (!await CargarJornadaAsync(jornadaId))
        {
            return NotFound();
        }

        await CargarDesplegablesAsync(Jornada.CompeticionId);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int jornadaId)
    {
        Partido.JornadaId = jornadaId;
        Partido.Estado = PartidoEstado.Programado;

        if (!await CargarJornadaAsync(jornadaId))
        {
            return NotFound();
        }

        if (Partido.EquipoLocalId == Partido.EquipoVisitanteId)
        {
            ModelState.AddModelError(string.Empty, MismoEquipoMensaje);
        }
        else if (await ExisteConflictoDeEquipoAsync(jornadaId))
        {
            ModelState.AddModelError(string.Empty, EquipoRepetidoMensaje);
        }

        if (!ModelState.IsValid)
        {
            await CargarDesplegablesAsync(Jornada.CompeticionId);
            return Page();
        }

        context.Partidos.Add(Partido);
        await context.SaveChangesAsync();

        return RedirectToPage("./Index", new { jornadaId });
    }

    private async Task<bool> ExisteConflictoDeEquipoAsync(int jornadaId)
    {
        var partidosDeLaJornada = await context.Partidos
            .Where(p => p.JornadaId == jornadaId)
            .Select(p => new { p.Id, p.EquipoLocalId, p.EquipoVisitanteId })
            .ToListAsync();

        return PartidoReglas.EquipoYaJuegaEnJornada(
            partidosDeLaJornada.Select(p => (p.Id, p.EquipoLocalId, p.EquipoVisitanteId)),
            partidoIdActual: 0,
            Partido.EquipoLocalId,
            Partido.EquipoVisitanteId);
    }

    private async Task<bool> CargarJornadaAsync(int jornadaId)
    {
        var jornada = await context.Jornadas
            .Include(j => j.Competicion).ThenInclude(c => c.Temporada)
            .Include(j => j.Competicion).ThenInclude(c => c.Categoria)
            .FirstOrDefaultAsync(j => j.Id == jornadaId);
        if (jornada is null)
        {
            return false;
        }

        Jornada = jornada;
        return true;
    }

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
