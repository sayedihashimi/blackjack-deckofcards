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
    private readonly IGameService _game;
    private readonly IGameStateSerializer _serializer;
    private readonly IGameRoundPersister _persister;
    private readonly IOptionsMonitor<BlackjackOptions> _options;
    private readonly AppDbContext _db;

    public GameStateSnapshot? Snapshot { get; private set; }

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
        return Page();
    }

    public async Task<IActionResult> OnPostNewGame()
    {
        try
        {
            Snapshot = _game.NewGame(1000m, 10m);
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
