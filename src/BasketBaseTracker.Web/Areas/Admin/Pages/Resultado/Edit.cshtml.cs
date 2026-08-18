using BasketBaseTracker.Web.Data;
using BasketBaseTracker.Web.Data.Entities;
using BasketBaseTracker.Web.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BasketBaseTracker.Web.Areas.Admin.Pages.Resultados;

public class EditModel(ApplicationDbContext context) : PageModel
{
    private const string MarcadorInvalidoMensaje = "Introduce un marcador sin empate para un partido jugado o resuelto.";
    private const string ResolucionInvalidaMensaje = "Para resolver el partido, indica el motivo y un equipo ganador que sea uno de los dos que juegan.";
    private const string ConflictoDeConcurrenciaMensaje = "Este partido se ha modificado en otro sitio mientras tanto. Recarga la página e inténtalo de nuevo.";

    // Datos de solo lectura para la cabecera y los enlaces — el formulario solo
    // envía Estado/marcador/motivo/ganador/Observaciones, así que esta información
    // no llega en el POST y hay que recargarla explícitamente cada vez.
    public Partido PartidoContexto { get; set; } = null!;

    [BindProperty]
    public Partido Partido { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var partido = await CargarPartidoAsync(id);
        if (partido is null)
        {
            return NotFound();
        }

        PartidoContexto = partido;
        Partido = partido;
        CargarDesplegables(partido);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        // Cargar y parchear (mismo patrón que Partido/Edit, BAS-9): este
        // formulario no cubre EquipoLocalId/EquipoVisitanteId/SedeId/FechaHora,
        // que pertenecen a Calendario.
        var partidoExistente = await CargarPartidoAsync(id);
        if (partidoExistente is null)
        {
            return NotFound();
        }

        PartidoContexto = partidoExistente;

        var requiereMarcador = ResultadoReglas.RequiereMarcador(Partido.Estado);
        var esResuelto = Partido.Estado == PartidoEstado.Resuelto;

        if (requiereMarcador && !ResultadoReglas.MarcadorValido(Partido.PuntosLocal, Partido.PuntosVisitante))
        {
            ModelState.AddModelError(string.Empty, MarcadorInvalidoMensaje);
        }

        if (esResuelto && !ResultadoReglas.ResolucionValida(Partido.MotivoResolucion, Partido.EquipoGanadorResolucionId, partidoExistente.EquipoLocalId, partidoExistente.EquipoVisitanteId))
        {
            ModelState.AddModelError(string.Empty, ResolucionInvalidaMensaje);
        }

        if (!ModelState.IsValid)
        {
            CargarDesplegables(partidoExistente);
            return Page();
        }

        partidoExistente.Estado = Partido.Estado;
        partidoExistente.PuntosLocal = requiereMarcador ? Partido.PuntosLocal : null;
        partidoExistente.PuntosVisitante = requiereMarcador ? Partido.PuntosVisitante : null;
        partidoExistente.MotivoResolucion = esResuelto ? Partido.MotivoResolucion : null;
        partidoExistente.EquipoGanadorResolucionId = esResuelto ? Partido.EquipoGanadorResolucionId : null;
        partidoExistente.Observaciones = Partido.Observaciones;

        // La entidad viene de una consulta trackeada de este mismo DbContext (no de
        // un grafo desconectado como en Equipo/Edit), así que su RowVersion en
        // memoria siempre coincidiría con el de BD y nunca dispararía el conflicto
        // de concurrencia — hay que fijar explícitamente el valor original que el
        // formulario tenía al abrirse para que EF Core lo compare de verdad
        // (plan.md).
        context.Entry(partidoExistente).Property(p => p.RowVersion).OriginalValue = Partido.RowVersion;

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            ModelState.AddModelError(string.Empty, ConflictoDeConcurrenciaMensaje);
            CargarDesplegables(partidoExistente);
            return Page();
        }

        return RedirectToPage("/Partido/Index", new { area = "Admin", jornadaId = partidoExistente.JornadaId });
    }

    private async Task<Partido?> CargarPartidoAsync(int id) =>
        await context.Partidos
            .Include(p => p.EquipoLocal)
            .Include(p => p.EquipoVisitante)
            .FirstOrDefaultAsync(p => p.Id == id);

    private void CargarDesplegables(Partido partido)
    {
        List<Equipo> equipos = [partido.EquipoLocal, partido.EquipoVisitante];
        ViewData["EquipoGanadorResolucionId"] = new SelectList(equipos, "Id", "Nombre", Partido.EquipoGanadorResolucionId);
    }
}
