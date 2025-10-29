using System.Threading.Tasks;
using Xunit;
using BlackjackRazor.Domain;
using BlackjackRazor.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;

namespace BlackjackRazor.Tests;

public class HiddenCardVisibilityTests
{
    private ServiceProvider Build(params Card[] shoeCards)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IShoeManager>(new SequenceShoe(shoeCards));
        services.AddSingleton<IDealerLogic, DealerLogic>();
        services.AddSingleton<IPayoutService, PayoutService>();
        services.AddSingleton<IGameService, GameService>();
        services.Configure<BlackjackOptions>(o => { o.DefaultDeckCount = 1; o.DealerHitSoft17 = false; });
        return services.BuildServiceProvider();
    }

    // Reuse minimal deterministic queue shoe used elsewhere
    private sealed class SequenceShoe : IShoeManager
    {
        private readonly Queue<Card> _queue;
        public SequenceShoe(IEnumerable<Card> cards) => _queue = new Queue<Card>(cards);
        public int Remaining => _queue.Count;
        public Task<IReadOnlyList<Card>> DrawAsync(int count, System.Threading.CancellationToken ct = default)
        {
            var list = new List<Card>(count);
            for (int i = 0; i < count && _queue.Count > 0; i++) list.Add(_queue.Dequeue());
            return Task.FromResult<IReadOnlyList<Card>>(list);
        }
    }

    [Fact]
    public async Task DealerHoleCardHiddenUntilDealerPhase()
    {
        var provider = Build(
            new Card(CardRank.Ten, CardSuit.Spades),  // P1
            new Card(CardRank.Nine, CardSuit.Clubs),  // D1
            new Card(CardRank.Eight, CardSuit.Hearts),// P2
            new Card(CardRank.Seven, CardSuit.Diamonds) // D2 (hole)
        );
        var game = provider.GetRequiredService<IGameService>();
        game.NewGame(1000m, 10m);
        var snap = await game.DealInitialAsync();
        Assert.Equal(GamePhase.PlayerActing, snap.Phase);
        Assert.False(snap.DealerPlayed);
        // UI logic hides index 1 card while in PlayerActing and DealerPlayed false
        Assert.Equal(2, snap.Dealer.Cards.Count);

        // Complete player action (stand) so dealer can act
        game.Stand();
        var dealerSnap = await game.AdvanceDealerAsync();
        Assert.True(dealerSnap.DealerPlayed);
    }
}
