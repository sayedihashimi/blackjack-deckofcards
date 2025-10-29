using System.Threading.Tasks;
using Xunit;
using BlackjackRazor.Domain;
using BlackjackRazor.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;

namespace BlackjackRazor.Tests;

public sealed class SequenceShoe : IShoeManager
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

public class GameServiceTests
{
    private ServiceProvider Build(params Card[] shoeCards)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IShoeManager>(new SequenceShoe(shoeCards));
        services.AddSingleton<IDealerLogic, DealerLogic>(); // Dealer will draw from same shoe if needed
        services.AddSingleton<IPayoutService, PayoutService>();
        services.AddSingleton<IGameService, GameService>();
        services.Configure<BlackjackOptions>(o => { o.DefaultDeckCount = 1; o.DealerHitSoft17 = false; });
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task FullRound_PlayerStands_DealerStands_Settles()
    {
        // Sequence: Player1, Dealer1, Player2, Dealer2 -> Player totals 18, dealer totals 17
        var provider = Build(
            new Card(CardRank.Ten, CardSuit.Spades), // P1
            new Card(CardRank.Nine, CardSuit.Clubs), // D1
            new Card(CardRank.Eight, CardSuit.Hearts), // P2
            new Card(CardRank.Seven, CardSuit.Spades) // D2
        );
        var game = provider.GetRequiredService<IGameService>();
        game.NewGame(500m, 50m);
        await game.DealInitialAsync();
        var snap = game.Stand();
        Assert.True(snap.PlayerHands[0].IsCompleted);
        await game.AdvanceDealerAsync();
        var finalSnap = game.SettleRound();
        Assert.Equal(550m, finalSnap.Bankroll); // player win +50
    }

    [Fact]
    public async Task PlayerBust_LosesBet()
    {
        var provider = Build(
            new Card(CardRank.Ten, CardSuit.Spades), // P1
            new Card(CardRank.Nine, CardSuit.Clubs), // D1
            new Card(CardRank.Eight, CardSuit.Hearts), // P2 -> player 18
            new Card(CardRank.Seven, CardSuit.Spades), // D2 -> dealer 16
            new Card(CardRank.King, CardSuit.Hearts) // Hit -> player bust 28
        );
        var game = provider.GetRequiredService<IGameService>();
        game.NewGame(200m, 25m);
        await game.DealInitialAsync();
        await game.HitAsync(); // player goes to 18
        await game.HitAsync(); // player busts
        await game.AdvanceDealerAsync();
        var final = game.SettleRound();
        Assert.Equal(175m, final.Bankroll);
    }

    [Fact]
    public async Task SplitPairCreatesTwoHands()
    {
        // Player: 8,8 then split; draw small cards.
        var provider = Build(
            new Card(CardRank.Eight, CardSuit.Spades),
            new Card(CardRank.Five, CardSuit.Hearts),
            new Card(CardRank.Eight, CardSuit.Clubs),
            new Card(CardRank.Six, CardSuit.Diamonds),
            new Card(CardRank.Two, CardSuit.Spades), // draw to first split hand
            new Card(CardRank.Three, CardSuit.Clubs)  // draw to second split hand
        );
        var game = provider.GetRequiredService<IGameService>();
        game.NewGame(300m, 10m);
        await game.DealInitialAsync();
        var afterSplit = await game.SplitAsync();
        Assert.Equal(2, afterSplit.PlayerHands.Count);
        Assert.True(afterSplit.PlayerHands[0].Cards.Count >= 2);
    }
}
