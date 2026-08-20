using BasketBaseTracker.Web.Data;
using BasketBaseTracker.Web.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

namespace BasketBaseTracker.Web.Areas.Public.Pages.Competiciones;

[OutputCache(PolicyName = "Publico")]
public class ClasificacionModel(ClasificacionService clasificacionService, ApplicationDbContext context) : PageModel
{
    public IReadOnlyList<FilaClasificacionVista> Tabla { get; set; } = [];

    public int TemporadaId { get; set; }

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        var tabla = await clasificacionService.ObtenerAsync(id, cancellationToken);
        if (tabla is null)
        {
            return NotFound();
        }

        Tabla = tabla;
        TemporadaId = await context.Competiciones
            .Where(c => c.Id == id)
            .Select(c => c.TemporadaId)
            .SingleAsync(cancellationToken);
        return Page();
    }
}
