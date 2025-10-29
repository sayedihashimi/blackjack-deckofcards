using System.Diagnostics;
using BlackjackRazor.Domain;
using Microsoft.Extensions.DependencyInjection;
using BlackjackRazor.Infrastructure;

namespace BlackjackRazor.Simulation;

/// <summary>
/// Runs headless blackjack rounds using simplified strategy for performance / memory profiling.
/// </summary>
public static class PerformanceSimulator
{
    /// <summary>
    /// Executes the specified number of rounds and returns basic metrics.
    /// </summary>
    public static SimulationResult Run(int rounds, decimal startingBankroll, decimal bet, bool dealerHitSoft17 = false, int deckCount = 6)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IShoeManager>(new LocalRandomShoeManager(deckCount));
        services.AddSingleton<IDealerLogic, DealerLogic>();
        services.AddSingleton<IPayoutService, PayoutService>();
        services.AddSingleton<IGameService, GameService>();
        services.Configure<BlackjackOptions>(o => { o.DefaultDeckCount = deckCount; o.DealerHitSoft17 = dealerHitSoft17; });
        var provider = services.BuildServiceProvider();
        var game = provider.GetRequiredService<IGameService>();

        decimal bankroll = startingBankroll;
        var sw = Stopwatch.StartNew();
        int playerBlackjacks = 0, dealerBusts = 0, roundsPlayed = 0;

        for (int i = 0; i < rounds; i++)
        {
            game.NewGame(bankroll, bet);
            var snap = game.DealInitialAsync().GetAwaiter().GetResult();
            // Natural blackjack: skip actions, proceed dealer if needed
            if (!snap.PlayerHands[0].IsCompleted)
            {
                // Simplified strategy: hit until total >= 12 then stand.
                while (true)
                {
                    var hand = game.RawContext.PlayerHands[game.RawContext.ActiveHandIndex];
                    var eval = HandEvaluator.Evaluate(hand);
                    if (eval.IsBust || eval.IsBlackjack || eval.Total >= 12)
                    {
                        if (!hand.IsCompleted) game.Stand();
                        // Advance to next hand or break when all done
                        if (game.RawContext.ActiveHandIndex >= game.RawContext.PlayerHands.Count) break;
                        if (game.RawContext.PlayerHands[game.RawContext.ActiveHandIndex].IsCompleted) break; // safety
                    }
                    else
                    {
                        game.HitAsync().GetAwaiter().GetResult();
                    }
                }
            }
            var dealerPhase = game.AdvanceDealerAsync().GetAwaiter().GetResult();
            var settled = game.SettleRound();
            bankroll = settled.Bankroll; // carry forward updated bankroll
            if (settled.Events.Any(e => e.Contains("blackjack", StringComparison.OrdinalIgnoreCase))) playerBlackjacks++;
            if (settled.Events.Any(e => e.StartsWith("Dealer bust"))) dealerBusts++;
            roundsPlayed++;
        }
        sw.Stop();
        return new SimulationResult(roundsPlayed, bankroll, sw.Elapsed, playerBlackjacks, dealerBusts);
    }
}

public sealed record SimulationResult(int Rounds, decimal FinalBankroll, TimeSpan Elapsed, int PlayerBlackjacks, int DealerBusts)
{
    public double RoundsPerSecond => Rounds / Elapsed.TotalSeconds;
    public override string ToString() => $"Rounds={Rounds} FinalBankroll={FinalBankroll} ElapsedMs={Elapsed.TotalMilliseconds:F0} RPS={RoundsPerSecond:F1} PlayerBJ={PlayerBlackjacks} DealerBusts={DealerBusts}";
}

/// <summary>
/// Lightweight local shoe avoiding external API; builds and shuffles N decks in-memory.
/// </summary>
internal sealed class LocalRandomShoeManager : IShoeManager
{
    private readonly int _deckCount;
    private readonly Queue<Card> _cards = new();
    private readonly object _lock = new();
    private readonly Random _rng = new(42); // deterministic seed for perf consistency

    public LocalRandomShoeManager(int deckCount) => _deckCount = deckCount <= 0 ? 6 : deckCount;

    public int Remaining => _cards.Count;

    public Task<IReadOnlyList<Card>> DrawAsync(int count, CancellationToken ct = default)
    {
        if (count <= 0) return Task.FromResult<IReadOnlyList<Card>>(Array.Empty<Card>());
        lock (_lock)
        {
            if (_cards.Count < count) Refill();
            var list = new List<Card>(count);
            for (int i = 0; i < count && _cards.Count > 0; i++) list.Add(_cards.Dequeue());
            return Task.FromResult<IReadOnlyList<Card>>(list);
        }
    }

    private void Refill()
    {
        var pool = new List<Card>(_deckCount * 52);
        for (int d = 0; d < _deckCount; d++)
        {
            foreach (CardSuit suit in Enum.GetValues(typeof(CardSuit)))
            {
                foreach (CardRank rank in Enum.GetValues(typeof(CardRank)))
                {
                    pool.Add(new Card(rank, suit));
                }
            }
        }
        // Fisher–Yates shuffle
        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = _rng.Next(i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }
        _cards.Clear();
        foreach (var c in pool) _cards.Enqueue(c);
    }
}
