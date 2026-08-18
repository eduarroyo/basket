using BasketBaseTracker.Web.Data;
using BasketBaseTracker.Web.Data.Entities;
using BasketBaseTracker.Web.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BasketBaseTracker.Web.Areas.Admin.Pages.Clasificaciones;

public class IndexModel(ApplicationDbContext context, ClasificacionService clasificacionService) : PageModel
{
    public Competicion Competicion { get; set; } = null!;

    public IReadOnlyList<FilaClasificacionVista> Tabla { get; set; } = [];

    public async Task<IActionResult> OnGetAsync(int competicionId, CancellationToken cancellationToken)
    {
        var competicion = await context.Competiciones
            .Include(c => c.Temporada)
            .Include(c => c.Categoria)
            .FirstOrDefaultAsync(c => c.Id == competicionId, cancellationToken);
        if (competicion is null)
        {
            return NotFound();
        }

        var tabla = await clasificacionService.ObtenerAsync(competicionId, cancellationToken);
        if (tabla is null)
        {
            return NotFound();
        }

        Competicion = competicion;
        Tabla = tabla;
        return Page();
    }
}
