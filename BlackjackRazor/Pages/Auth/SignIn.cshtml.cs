using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

public class SignInModel : PageModel
{
    [BindProperty]
    public string Username { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync([FromServices] BlackjackRazor.Data.AppDbContext db)
    {
        if (string.IsNullOrWhiteSpace(Username))
        {
            ModelState.AddModelError("Username", "Username is required.");
            return Page();
        }

        var claims = new List<Claim> { new Claim(ClaimTypes.Name, Username) };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        var existing = db.Users.FirstOrDefault(u => u.Username == Username);
        if (existing == null)
        {
            existing = new BlackjackRazor.Data.User { Username = Username };
            db.Users.Add(existing);
            await db.SaveChangesAsync();
            var s = new BlackjackRazor.Data.Stat { UserId = existing.Id };
            db.Stats.Add(s);
            await db.SaveChangesAsync();
        }

        return RedirectToPage("/Index");
    }
}
