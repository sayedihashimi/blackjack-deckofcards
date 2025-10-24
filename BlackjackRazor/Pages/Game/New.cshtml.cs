using BlackjackRazor.Models;
using BlackjackRazor.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class GameNewModel : PageModel
{
    private readonly DeckApiClient _deck;
    private readonly IGameSessionStore _store;
    public int DeckCount { get; set; } = 6;
    public int DefaultBet { get; set; } = 10;

    public GameNewModel(DeckApiClient deck, IGameSessionStore store)
    {
        _deck = deck; _store = store;
    }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(int deckCount, int defaultBet)
    {
        if (!(User?.Identity?.IsAuthenticated ?? false)) return RedirectToPage("/Auth/SignIn");
        var username = User.Identity!.Name!;
        var state = _store.GetOrCreate(username);
        state.Game.DeckCount = deckCount <=0 ? 6 : deckCount;
        state.Game.DefaultBet = defaultBet <=0 ? 10 : defaultBet;
        state.Game.DeckId = await _deck.NewShuffledDeckAsync(state.Game.DeckCount);
        state.Game.HandInProgress = false;
        return RedirectToPage("/Game/Play");
    }
}
