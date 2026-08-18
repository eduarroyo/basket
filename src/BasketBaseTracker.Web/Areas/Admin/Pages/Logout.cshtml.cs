using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BasketBaseTracker.Web.Areas.Admin.Pages;

public class LogoutModel(SignInManager<IdentityUser> signInManager, ILogger<LogoutModel> logger) : PageModel
{
    // El menú del área Admin (BAS-6) ya ofrece "Cerrar sesión" en todas las páginas
    // autenticadas; si se llega aquí por GET con sesión activa, se redirige en vez de
    // mostrar un segundo botón redundante con el del menú.
    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated ?? false)
        {
            return RedirectToPage("/Index", new { area = "Admin" });
        }

        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        await signInManager.SignOutAsync();
        logger.LogInformation("El usuario ha cerrado sesión.");

        return LocalRedirect(Url.Content("~/"));
    }
}
