using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BlackjackRazor.Pages;

public class IndexModel : PageModel
{
    public IActionResult OnGet()
    {
        // If not authenticated, send to Sign In.
        if (!(User.Identity?.IsAuthenticated ?? false))
        {
            return RedirectToPage("/Auth/SignIn");
        }
        // Authenticated: redirect directly to Play page (primary experience)
        return RedirectToPage("/Game/Play");
    }
}
