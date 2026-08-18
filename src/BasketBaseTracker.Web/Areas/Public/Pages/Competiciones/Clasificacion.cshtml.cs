using BasketBaseTracker.Web.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;

namespace BasketBaseTracker.Web.Areas.Public.Pages.Competiciones;

[OutputCache(PolicyName = "Publico")]
public class ClasificacionModel(ClasificacionService clasificacionService) : PageModel
{
    public IReadOnlyList<FilaClasificacionVista> Tabla { get; set; } = [];

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        var tabla = await clasificacionService.ObtenerAsync(id, cancellationToken);
        if (tabla is null)
        {
            return NotFound();
        }

        Tabla = tabla;
        return Page();
    }
}
