using System.Threading.Tasks;
using BlackjackRazor.Domain;
using BlackjackRazor.Infrastructure;
using BlackjackRazor.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public class AllHandsBustNoDealerPlayTests
{
    private ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IShoeManager, StubShoeManagerAllBust>();
        services.AddSingleton<IPayoutService, PayoutService>();
        services.AddSingleton<IDealerLogic, DealerLogic>();
        services.AddSingleton<IGameService, GameService>();
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task Dealer_Does_Not_Play_When_All_Player_Hands_Bust_Auto_Settled()
    {
        var provider = BuildProvider();
        var game = provider.GetRequiredService<IGameService>();
        var snap = game.NewGame(1000m, 10m);
        snap = await game.DealInitialAsync();
        // Hit until bust
        while (snap.Phase == GamePhase.PlayerActing && snap.ActiveHandIndex < snap.PlayerHands.Count && !snap.PlayerHands[snap.ActiveHandIndex].Evaluation.IsBust)
        {
            snap = await game.HitAsync();
        }
        // After bust auto settlement should have occurred
        Assert.Equal(GamePhase.Settled, snap.Phase);
        Assert.False(snap.DealerPlayed, "Dealer should not have played when all player hands bust.");
        Assert.Contains(snap.Events, e => e.Contains("All player hands bust"));
        // Net should be negative bet (loss)
        Assert.Equal(-10m, snap.RoundNetDelta);
        // Settlement results should reflect player bust outcome
        Assert.Single(snap.SettlementResults);
        Assert.Equal(HandOutcome.Bust, snap.SettlementResults[0].Outcome);
    }

    private sealed class StubShoeManagerAllBust : IShoeManager
    {
        private readonly Queue<Card> _cards = new Queue<Card>();
        public int Remaining => _cards.Count;
        public StubShoeManagerAllBust()
        {
            // Initial deal: player, dealer, player, dealer -> player gets 10 + 10; dealer gets small cards
            _cards.Enqueue(new Card(CardRank.Ten, CardSuit.Hearts));
            _cards.Enqueue(new Card(CardRank.Three, CardSuit.Clubs));
            _cards.Enqueue(new Card(CardRank.Ten, CardSuit.Spades));
            _cards.Enqueue(new Card(CardRank.Five, CardSuit.Diamonds));
            // Next hit card to bust (King = total 30)
            _cards.Enqueue(new Card(CardRank.King, CardSuit.Clubs));
        }
        public Task<IReadOnlyList<Card>> DrawAsync(int count, System.Threading.CancellationToken ct = default)
        {
            var list = new List<Card>(count);
            for (int i = 0; i < count && _cards.Count > 0; i++)
            {
                list.Add(_cards.Dequeue());
            }
            return Task.FromResult<IReadOnlyList<Card>>(list);
        }
    }
}
