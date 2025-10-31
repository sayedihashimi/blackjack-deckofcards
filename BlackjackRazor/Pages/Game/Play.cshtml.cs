using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using BlackjackRazor.Domain;
using BlackjackRazor.Persistence;
using BlackjackRazor.Infrastructure;
using Microsoft.Extensions.Options;
using BlackjackRazor.Data;

namespace BlackjackRazor.Pages.Game;

public class PlayModel : PageModel
{
    [BindProperty]
    public decimal BetAmount { get; set; } = 10;
    private readonly IGameService _game;
    private readonly IGameStateSerializer _serializer;
    private readonly IGameRoundPersister _persister;
    private readonly IOptionsMonitor<BlackjackOptions> _options;
    private readonly AppDbContext _db;

    public GameStateSnapshot? Snapshot { get; private set; }
    public int DealerDelayMs => _options.CurrentValue.DealerCardDelayMs;

    public PlayModel(IGameService game, IGameStateSerializer serializer, IGameRoundPersister persister, IOptionsMonitor<BlackjackOptions> options, AppDbContext db)
    {
        _game = game;
        _serializer = serializer;
        _persister = persister;
        _options = options;
        _db = db;
    }

    public IActionResult OnGet()
    {
        LoadSnapshotAndContext();
        // Set BetAmount to previous bet if available, else default to 10
        if (Snapshot != null && Snapshot.CurrentBet > 0)
        {
            BetAmount = Snapshot.CurrentBet;
        }
        else
        {
            BetAmount = 10;
        }
        return Page();
    }

    public async Task<IActionResult> OnPostNewGame()
    {
        try
        {
            var bet = BetAmount > 0 ? BetAmount : 10m;
            Snapshot = _game.NewGame(1000m, bet);
            // Persist initial game row
            var username = HttpContext.Session.GetString("Username") ?? User.Identity?.Name ?? "Guest";
            var deckCount = _options.CurrentValue.DefaultDeckCount;
            var gameId = await _persister.EnsureGameAsync(Snapshot, username, deckCount, _db);
            _game.SetGameId(gameId);
            Snapshot = _game.Refresh();
            Persist();
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeal()
    {
        try
        {
            LoadSnapshotAndContext();
            Snapshot = await _game.DealInitialAsync();
            Persist();
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostHit()
    {
        try
        {
            LoadSnapshotAndContext();
            Snapshot = await _game.HitAsync();
            Persist();
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToPage();
    }

    public IActionResult OnPostStand()
    {
        try
        {
            LoadSnapshotAndContext();
            Snapshot = _game.Stand();
            Persist();
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSplit()
    {
        try
        {
            LoadSnapshotAndContext();
            Snapshot = await _game.SplitAsync();
            Persist();
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDouble()
    {
        try
        {
            LoadSnapshotAndContext();
            Snapshot = await _game.DoubleAsync();
            Persist();
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDealer()
    {
        try
        {
            LoadSnapshotAndContext();
            Snapshot = await _game.AdvanceDealerAsync();
            Persist();
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToPage();
    }

    /// <summary>
    /// Incremental dealer step endpoint used by auto-play script. Returns JSON snapshot summary.
    /// </summary>
    public async Task<IActionResult> OnPostDealerStep()
    {
        try
        {
            LoadSnapshotAndContext();
            Snapshot = await _game.DealerStepAsync();
            Persist();
            if (Snapshot == null) return new JsonResult(new { ok = false });
            var dealer = Snapshot.Dealer;
            var cardCodes = dealer.Cards.Select(c => c.ToShortCode()).ToArray();
            bool conceal = !(Snapshot.DealerPlayed || Snapshot.Phase == GamePhase.DealerActing);
            return new JsonResult(new
            {
                ok = true,
                phase = Snapshot.Phase.ToString(),
                dealerPlayed = Snapshot.DealerPlayed,
                cards = cardCodes,
                conceal,
                total = dealer.Evaluation.Total,
                soft = dealer.Evaluation.IsSoft,
                bust = dealer.Evaluation.IsBust,
                more = Snapshot.Phase == GamePhase.DealerActing // still acting means more steps
            });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { ok = false, error = ex.Message });
        }
    }

    public async Task<IActionResult> OnPostSettle()
    {
        try
        {
            LoadSnapshotAndContext();
            Snapshot = _game.SettleRound();
            await _persister.PersistSettlementAsync(Snapshot, _db);
            Persist();
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToPage();
    }

    private void LoadSnapshotAndContext()
    {
        Snapshot = _serializer.Load(HttpContext.Session);
        if (Snapshot is null)
        {
            Snapshot = _game.NewGame(1000m, 10m);
        }
        else
        {
            // Rehydrate service state for this request scope
            _game.Load(Snapshot);
        }
        Persist();
    }

    private void Persist()
    {
        if (Snapshot != null)
        {
            _serializer.Save(HttpContext.Session, Snapshot);
        }
    }
}
