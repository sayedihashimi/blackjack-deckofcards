using BlackjackRazor.Data;
using BlackjackRazor.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BlackjackRazor.Persistence;

public interface IGameRoundPersister
{
    Task<int> EnsureGameAsync(GameStateSnapshot snapshot, string username, int deckCount, AppDbContext db, CancellationToken ct = default);
    Task PersistSettlementAsync(GameStateSnapshot snapshot, AppDbContext db, CancellationToken ct = default);
}

public sealed class EfGameRoundPersister : IGameRoundPersister
{
    private readonly IOptionsMonitor<BlackjackRazor.Infrastructure.BlackjackOptions> _options;
    public EfGameRoundPersister(IOptionsMonitor<BlackjackRazor.Infrastructure.BlackjackOptions> options) => _options = options;

    public async Task<int> EnsureGameAsync(GameStateSnapshot snapshot, string username, int deckCount, AppDbContext db, CancellationToken ct = default)
    {
        // Find or create user
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username, ct);
        if (user == null)
        {
            user = new User { Username = username };
            db.Users.Add(user);
            await db.SaveChangesAsync(ct);
        }

        if (snapshot.GameId.HasValue)
        {
            var existing = await db.Games.FirstOrDefaultAsync(g => g.Id == snapshot.GameId.Value, ct);
            if (existing != null) return existing.Id;
        }

        var game = new Game
        {
            UserId = user.Id,
            DeckCount = deckCount,
            DeckId = string.Empty, // Could populate from shoe manager later
            DefaultBet = (int)snapshot.CurrentBet,
            BankrollStart = snapshot.Bankroll,
            BankrollEnd = snapshot.Bankroll,
            HandsPlayed = 0
        };
        db.Games.Add(game);
        await db.SaveChangesAsync(ct);
        return game.Id;
    }

    public async Task PersistSettlementAsync(GameStateSnapshot snapshot, AppDbContext db, CancellationToken ct = default)
    {
        if (!snapshot.GameId.HasValue) return; // nothing to persist yet
        var game = await db.Games.FirstOrDefaultAsync(g => g.Id == snapshot.GameId.Value, ct);
        if (game == null) return;

        // Update bankroll end
        game.BankrollEnd = snapshot.Bankroll;

        foreach (var r in snapshot.SettlementResults)
        {
            // Avoid duplicate insert (idempotent) by checking existing hand record
            bool exists = await db.Hands.AnyAsync(h => h.GameId == game.Id && h.HandIndex == r.HandIndex, ct);
            if (exists) continue;
            // Outcome stats
            switch (r.Outcome)
            {
                case HandOutcome.Blackjack: game.PlayerBlackjacks++; game.PlayerWins++; break;
                case HandOutcome.Win: game.PlayerWins++; break;
                case HandOutcome.Lose: game.DealerWins++; break;
                case HandOutcome.Push: game.Pushes++; break;
                case HandOutcome.Bust: game.PlayerBusts++; game.DealerWins++; break;
            }
            var hand = new Data.Hand
            {
                GameId = game.Id,
                HandIndex = r.HandIndex,
                Bet = r.Bet,
                Outcome = r.Outcome,
                Payout = r.Payout,
                WasSplit = snapshot.PlayerHands[r.HandIndex].WasSplitChild,
                WasDouble = snapshot.PlayerHands[r.HandIndex].HasDoubled
            };
            db.Hands.Add(hand);
        }

        game.HandsPlayed = await db.Hands.CountAsync(h => h.GameId == game.Id, ct);
        await db.SaveChangesAsync(ct);
    }
}
