using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BlackjackRazor.Models;
using BlackjackRazor.Services;
using System.Threading.Tasks;

namespace BlackjackRazor.Pages.Game;

public class NewGameModel : PageModel
{
    private readonly BlackjackEngine _engine;
    public NewGameModel(BlackjackEngine engine) { _engine = engine; }

    [BindProperty] public int DeckCount { get; set; } = 6;
    [BindProperty] public decimal DefaultBet { get; set; } = 10m;
    [BindProperty] public decimal Bankroll { get; set; } = 500m;

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        var state = new GameState { DeckCount = DeckCount, DefaultBet = DefaultBet, Bankroll = Bankroll };
        await _engine.InitializeAsync(state);
        HttpContext.Session.Set("game", System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(state));
        return RedirectToPage("/Game/Play");
    }
}
