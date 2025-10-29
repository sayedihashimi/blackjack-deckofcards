using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BlackjackRazor.Services;

namespace BlackjackRazor.Pages.Auth;

public class SignInModel : PageModel
{
    private readonly IUserStore _userStore;
    public SignInModel(IUserStore userStore) => _userStore = userStore;

    [BindProperty]
    public string Username { get; set; } = string.Empty;
    public string? Error { get; set; }

    private static readonly Regex UsernameRegex = new("^[A-Za-z0-9_]{2,24}$", RegexOptions.Compiled);

    public void OnGet()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            Response.Redirect("/Game/New"); // placeholder destination
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!UsernameRegex.IsMatch(Username))
        {
            Error = "Invalid username format.";
            return Page();
        }

        var userRecord = await _userStore.UpsertAsync(Username);
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, userRecord.Username),
            new Claim("createdUtc", userRecord.CreatedUtc.ToString("O"))
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        HttpContext.Session.SetString("Username", userRecord.Username);

        return Redirect("/");
    }
}
