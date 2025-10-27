using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BlackjackRazor.Models;
using BlackjackRazor.Services;
using BlackjackRazor.Data;
using System.Threading.Tasks;
using System.Text.Json;
using System.Linq;
using System.Security.Claims;

namespace BlackjackRazor.Pages.Game;

public class PlayGameModel : PageModel
{
    private readonly BlackjackEngine _engine;
    private readonly AppDbContext _db;
    public GameState State { get; set; } = new();
    public SessionStats Stats { get; set; } = new();
    public PlayGameModel(BlackjackEngine engine, AppDbContext db) { _engine = engine; _db = db; }

    public void OnGet()
    {
        Load();
    }

    private void Load()
    {
        var data = HttpContext.Session.Get("game");
        if (data != null) State = JsonSerializer.Deserialize<GameState>(data) ?? new GameState();
        var statsData = HttpContext.Session.Get("stats");
        if (statsData != null) Stats = JsonSerializer.Deserialize<SessionStats>(statsData) ?? new SessionStats();
    }

    private void Save()
    {
        HttpContext.Session.Set("game", JsonSerializer.SerializeToUtf8Bytes(State));
        HttpContext.Session.Set("stats", JsonSerializer.SerializeToUtf8Bytes(Stats));
    }

    public bool ShowHandButtons(int i) => i == State.ActiveHandIndex && !State.GameResolved && !State.DealerPlaying;
    public bool CanHit(int i) { if (i!=State.ActiveHandIndex) return false; return _engine.CanHit(State); }
    public bool CanStand(int i) { if (i!=State.ActiveHandIndex) return false; return _engine.CanStand(State); }
    public bool CanDouble(int i) { if (i!=State.ActiveHandIndex) return false; return _engine.CanDouble(State); }
    public bool CanSplit(int i) { return i==0 && _engine.CanSplit(State); }

    public async Task<IActionResult> OnPostAsync(string action, int handIndex)
    {
        Load();
        State.ActiveHandIndex = handIndex;
        switch(action){
            case "hit": await _engine.HitAsync(State); break;
            case "stand": await _engine.StandAsync(State); break;
            case "double": await _engine.DoubleDownAsync(State); break;
            case "split": await _engine.SplitAsync(State); break;
        }
        if (State.GameResolved)
        {
            Stats.HandsPlayed += State.Hands.Count;
            Stats.HandsWon += State.Hands.Count(h => h.Outcome == "Win" || h.Outcome == "Blackjack");
            Stats.HandsLost += State.Hands.Count(h => h.Outcome == "Lose" || h.Outcome == "Bust");
            Stats.HandsPushed += State.Hands.Count(h => h.Outcome == "Push");
            Stats.NetAmount = State.Bankroll - 500m; // baseline if started at 500
            PersistRound();
        }
        Save();
        return RedirectToPage();
    }

    private void PersistRound()
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username)) return;
        var user = _db.Users.FirstOrDefault(u => u.Username == username);
        if (user == null) return;
        var session = new GameSession
        {
            UserId = user.Id,
            DeckCount = State.DeckCount,
            DefaultBet = State.DefaultBet,
            DeckId = State.DeckId,
            Bankroll = State.Bankroll,
            EndedAt = System.DateTime.UtcNow
        };
        _db.GameSessions.Add(session);
        _db.SaveChanges();
        int handIndex = 0;
        foreach (var h in State.Hands)
        {
            var rec = new HandRecord
            {
                GameSessionId = session.Id,
                Index = handIndex++,
                Bet = h.Bet,
                Outcome = h.Outcome,
                Payout = h.Outcome switch
                {
                    "Blackjack" => h.Bet * 1.5m,
                    "Win" => h.Bet,
                    "Lose" => -h.Bet,
                    "Bust" => -h.Bet,
                    _ => 0m
                },
                IsSplitHand = h.IsSplitHand,
                IsBlackjack = h.Outcome == "Blackjack"
            };
            _db.Hands.Add(rec);
        }
        int order = 0;
        for (int i = 0; i < State.Hands.Count; i++)
        {
            foreach (var c in State.Hands[i].Cards)
            {
                _db.Cards.Add(new CardDrawn
                {
                    GameSessionId = session.Id,
                    HandIndex = i,
                    IsDealer = false,
                    Code = c.Code,
                    ImageUrl = c.ImageUrl,
                    Order = order++,
                    Value = c.Value
                });
            }
        }
        foreach (var c in State.Dealer.Cards)
        {
            _db.Cards.Add(new CardDrawn
            {
                GameSessionId = session.Id,
                HandIndex = -1,
                IsDealer = true,
                Code = c.Code,
                ImageUrl = c.ImageUrl,
                Order = order++,
                Value = c.Value
            });
        }
        var statsRecord = new StatRecord
        {
            UserId = user.Id,
            HandsPlayed = State.Hands.Count,
            HandsWon = State.Hands.Count(h => h.Outcome == "Win" || h.Outcome == "Blackjack"),
            HandsLost = State.Hands.Count(h => h.Outcome == "Lose" || h.Outcome == "Bust"),
            HandsPushed = State.Hands.Count(h => h.Outcome == "Push"),
            NetAmount = State.RoundNet
        };
        _db.Stats.Add(statsRecord);
        _db.SaveChanges();
    }
}
