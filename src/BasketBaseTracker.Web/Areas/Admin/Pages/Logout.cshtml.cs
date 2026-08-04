using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BasketBaseTracker.Web.Areas.Admin.Pages;

public class LogoutModel(SignInManager<IdentityUser> signInManager, ILogger<LogoutModel> logger) : PageModel
{
    public async Task<IActionResult> OnPost()
    {
        await signInManager.SignOutAsync();
        logger.LogInformation("El usuario ha cerrado sesión.");

        return LocalRedirect(Url.Content("~/"));
    }
}
