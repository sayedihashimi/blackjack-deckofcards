using System.Threading.Tasks;
using System.Collections.Generic;
using BlackjackRazor.Domain;
using BlackjackRazor.Infrastructure;
using BlackjackRazor.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public class AllHandsBustDealerRevealTests
{
    private ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddOptions();
        services.Configure<BlackjackOptions>(o => { });
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
        // Force player to hit until bust via stub shoe
        while (snap.Phase == GamePhase.PlayerActing && snap.ActiveHandIndex < snap.PlayerHands.Count && !snap.PlayerHands[snap.ActiveHandIndex].Evaluation.IsBust)
        {
            snap = await game.HitAsync();
        }
        // After bust, auto settlement occurs WITHOUT dealer playing.
        Assert.False(snap.DealerPlayed);
        Assert.Equal(GamePhase.Settled, snap.Phase);
        Assert.Contains(snap.Events, e => e.Contains("All player hands bust"));
        // Dealer hole card should remain concealed (DealerPlayed false)
        Assert.True(snap.Dealer.Cards.Count >= 2);
    }

    // Stub shoe manager that produces deterministic bust scenario: player draws high cards leading to bust quickly.
    private sealed class StubShoeManagerAllBust : IShoeManager
    {
        private readonly Queue<Card> _cards = new Queue<Card>();
        public int Remaining => _cards.Count;

        public StubShoeManagerAllBust()
        {
            // Initial deal: player, dealer, player, dealer -> player gets 10 + 10; dealer gets low cards.
            _cards.Enqueue(new Card(CardRank.Ten, CardSuit.Hearts)); // player1 card1
            _cards.Enqueue(new Card(CardRank.Three, CardSuit.Clubs)); // dealer card1
            _cards.Enqueue(new Card(CardRank.Ten, CardSuit.Spades)); // player1 card2
            _cards.Enqueue(new Card(CardRank.Five, CardSuit.Diamonds)); // dealer card2
            // Subsequent hits for player to bust: add a King.
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
