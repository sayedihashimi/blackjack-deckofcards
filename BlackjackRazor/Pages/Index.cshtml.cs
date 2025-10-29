using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BlackjackRazor.Pages;

public class IndexModel : PageModel
{
    public IActionResult OnGet()
    {
        if (!(User.Identity?.IsAuthenticated ?? false))
        {
            return RedirectToPage("/Auth/SignIn");
        }
        return Page();
    }
}
