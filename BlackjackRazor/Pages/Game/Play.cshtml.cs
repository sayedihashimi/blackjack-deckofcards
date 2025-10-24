using BlackjackRazor.Models;
using BlackjackRazor.Engine;
using BlackjackRazor.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class GamePlayModel : PageModel
{
    private readonly IGameSessionStore _store;
    private readonly DeckApiClient _deck;
    public UserSessionState? State { get; set; }
    public bool CanHit { get; set; }
    public bool CanStand { get; set; }
    public bool CanSplit { get; set; }
    public bool CanDouble { get; set; }

    public GamePlayModel(IGameSessionStore store, DeckApiClient deck)
    { _store = store; _deck = deck; }

    public void OnGet()
    {
        if (!(User?.Identity?.IsAuthenticated ?? false)) return;
        State = _store.GetOrCreate(User.Identity!.Name!);
        RecalcActions();
    }

    public async Task<IActionResult> OnPostDealAsync([FromServices] BlackjackRazor.Data.AppDbContext db)
    {
        if (!(User?.Identity?.IsAuthenticated ?? false)) return RedirectToPage("/Auth/SignIn");
        State = _store.GetOrCreate(User.Identity!.Name!);
        if (string.IsNullOrEmpty(State.Game.DeckId))
            State.Game.DeckId = await _deck.NewShuffledDeckAsync(State.Game.DeckCount);
        // Deduct bet from bankroll now
        State.Stats.Bankroll -= State.Game.DefaultBet;
        await BlackjackEngine.DealAsync(State.Game, _deck, State.Game.DefaultBet);
        RecalcActions();
        return Page();
    }

    public async Task<IActionResult> OnPostHitAsync([FromServices] BlackjackRazor.Data.AppDbContext db)
    {
        State = _store.GetOrCreate(User.Identity!.Name!);
        await BlackjackEngine.HitAsync(State.Game, _deck);
        await TryFinalizeAsync(db);
        RecalcActions();
        return Page();
    }

    public async Task<IActionResult> OnPostStandAsync([FromServices] BlackjackRazor.Data.AppDbContext db)
    {
        State = _store.GetOrCreate(User.Identity!.Name!);
        await BlackjackEngine.StandAsync(State.Game, _deck);
        await TryFinalizeAsync(db);
        RecalcActions();
        return Page();
    }

    public async Task<IActionResult> OnPostSplitAsync([FromServices] BlackjackRazor.Data.AppDbContext db)
    {
        State = _store.GetOrCreate(User.Identity!.Name!);
        await BlackjackEngine.SplitAsync(State.Game, _deck);
        RecalcActions();
        return Page();
    }

    public async Task<IActionResult> OnPostDoubleAsync([FromServices] BlackjackRazor.Data.AppDbContext db)
    {
        State = _store.GetOrCreate(User.Identity!.Name!);
        await BlackjackEngine.DoubleDownAsync(State.Game, _deck);
        await TryFinalizeAsync(db);
        RecalcActions();
        return Page();
    }

    public IActionResult OnPostNew()
    {
        State = _store.GetOrCreate(User.Identity!.Name!);
        State.Game.HandInProgress = false; // next Deal starts new hand
        State.Game.PlayerHands.Clear();
        State.Game.DealerCards.Clear();
        RecalcActions();
        return Page();
    }

    private void RecalcActions()
    {
        if (State == null) return;
        if (!State.Game.HandInProgress || State.Game.PlayerHands.Count==0)
        { CanHit = CanStand = CanSplit = CanDouble = false; return; }
        var valid = BlackjackEngine.ValidActions(State.Game).ToHashSet();
        CanHit = valid.Contains(ActionType.Hit);
        CanStand = valid.Contains(ActionType.Stand);
        CanSplit = valid.Contains(ActionType.Split);
        CanDouble = valid.Contains(ActionType.DoubleDown);
    }

    private async Task TryFinalizeAsync(BlackjackRazor.Data.AppDbContext db)
    {
        if (State == null) return;
        if (State.Game.HandInProgress) return; // still going
        // Round ended: compute payouts and update stats
        var dealerHv = BlackjackEngine.Evaluate(State.Game.DealerCards);
        var username = User.Identity!.Name!;
        var user = db.Users.FirstOrDefault(u => u.Username == username);
        int totalNet = 0;
        List<string> handSummaries = new();
        foreach (var hand in State.Game.PlayerHands)
        {
            var hv = BlackjackEngine.Evaluate(hand.Cards);
            var payout = BlackjackEngine.Payout(hand, hv, dealerHv);
            if (payout > 0) State.Stats.Bankroll += payout; // includes original bet returned
            totalNet += (payout - hand.Bet);
            State.Stats.HandsPlayed++;
            switch (hand.Result)
            {
                case HandResult.Blackjack: State.Stats.Blackjacks++; State.Stats.HandsWon++; break;
                case HandResult.Win: State.Stats.HandsWon++; break;
                case HandResult.Lose: State.Stats.HandsLost++; break;
                case HandResult.Push: State.Stats.HandsPushed++; break;
                case HandResult.Bust: State.Stats.HandsLost++; break;
            }
            handSummaries.Add($"Hand {State.Game.PlayerHands.IndexOf(hand)+1}: {hand.Result} (Bet ${hand.Bet}, Net {(payout - hand.Bet >=0?"+":"")}{payout - hand.Bet})");
            if (user != null)
            {
                var stat = db.Stats.FirstOrDefault(s => s.UserId == user.Id);
                if (stat != null)
                {
                    stat.GamesPlayed++;
                    if (hand.Result is HandResult.Blackjack) { stat.Blackjacks++; stat.GamesWon++; }
                    else if (hand.Result is HandResult.Win) stat.GamesWon++;
                    else if (hand.Result is HandResult.Lose or HandResult.Bust) stat.GamesLost++;
                    stat.TotalBet += hand.Bet;
                    stat.TotalPayout += payout;
                }
                db.GameHistories.Add(new BlackjackRazor.Data.GameHistory
                {
                    UserId = user.Id,
                    PlayedAt = DateTime.UtcNow,
                    Bet = hand.Bet,
                    Payout = payout,
                    Result = hand.Result.ToString()!
                });
            }
        }
        if (user != null) await db.SaveChangesAsync();
        State.Stats.LastNet = totalNet;
        State.Stats.LastSummary = string.Join("; ", handSummaries);
    }
}
