using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BlackjackRazor.Data;
using System.Threading.Tasks;
using System.Linq;

namespace BlackjackRazor.Pages;

public class SignInModel : PageModel
{
    private readonly AppDbContext _db;
    [BindProperty]
    public string Username { get; set; } = string.Empty;
    public SignInModel(AppDbContext db) { _db = db; }
    public void OnGet() { }
    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Username)) return Page();
        var existing = _db.Users.FirstOrDefault(u => u.Username == Username);
        if (existing == null)
        {
            existing = new User { Username = Username };
            _db.Users.Add(existing);
            await _db.SaveChangesAsync();
        }
        var claims = new[] { new Claim(ClaimTypes.Name, Username) };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
        return RedirectToPage("/Index");
    }
}
