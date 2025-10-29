using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BlackjackRazor.Domain;
using BlackjackRazor.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace BlackjackRazor.Tests;

public sealed class StubShoeManager : IShoeManager
{
    private readonly Queue<Card> _queue;
    public StubShoeManager(IEnumerable<Card> cards) => _queue = new Queue<Card>(cards);
    public int Remaining => _queue.Count;
    public Task<IReadOnlyList<Card>> DrawAsync(int count, CancellationToken ct = default)
    {
        var list = new List<Card>(count);
        for (int i=0; i<count && _queue.Count>0; i++) list.Add(_queue.Dequeue());
        return Task.FromResult<IReadOnlyList<Card>>(list);
    }
}

public class DealerLogicTests
{
    private ServiceProvider BuildProvider(bool hitSoft17, params Card[] shoeCards)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IShoeManager>(new StubShoeManager(shoeCards));
        services.Configure<BlackjackOptions>(o => { o.DefaultDeckCount = 1; o.DealerHitSoft17 = hitSoft17; });
        services.AddSingleton<IDealerLogic, DealerLogic>();
        return services.BuildServiceProvider();
    }

    private static Hand MakeHand(params Card[] cards)
    {
        var h = new Hand();
        foreach (var c in cards) h.AddCard(c);
        return h;
    }

    [Fact]
    public async Task Dealer_StandsOnHard17()
    {
        // Dealer starting with 10 + 7 should stand immediately.
        var provider = BuildProvider(hitSoft17:false);
        var logic = provider.GetRequiredService<IDealerLogic>();
        var hand = MakeHand(new Card(CardRank.Ten, CardSuit.Spades), new Card(CardRank.Seven, CardSuit.Clubs));
        var result = await logic.PlayAsync(hand);
        Assert.Equal(1, result.Steps.Count); // initial only
        Assert.Equal(17, result.FinalEvaluation.Total);
    }

    [Fact]
    public async Task Dealer_HitsSoft17_WhenConfigured()
    {
        // Dealer has Ace + Six (soft 17) and hits, drawing a small card.
        var provider = BuildProvider(hitSoft17:true, new Card(CardRank.Two, CardSuit.Diamonds));
        var logic = provider.GetRequiredService<IDealerLogic>();
        var hand = MakeHand(new Card(CardRank.Ace, CardSuit.Spades), new Card(CardRank.Six, CardSuit.Hearts));
        var result = await logic.PlayAsync(hand);
        Assert.True(result.Steps.Count > 1); // drew at least once
        Assert.Equal(19, result.FinalEvaluation.Total); //  A(11)+6+2=19
    }

    [Fact]
    public async Task Dealer_StandsOnSoft17_WhenNotConfigured()
    {
        var provider = BuildProvider(hitSoft17:false, new Card(CardRank.Two, CardSuit.Diamonds));
        var logic = provider.GetRequiredService<IDealerLogic>();
        var hand = MakeHand(new Card(CardRank.Ace, CardSuit.Spades), new Card(CardRank.Six, CardSuit.Hearts));
        var result = await logic.PlayAsync(hand);
        Assert.Equal(1, result.Steps.Count); // did not draw
        Assert.Equal(17, result.FinalEvaluation.Total);
    }

    [Fact]
    public async Task Dealer_StopsAfterBust()
    {
        // Dealer draws until bust.
        var provider = BuildProvider(hitSoft17:true,
            new Card(CardRank.Ten, CardSuit.Diamonds),
            new Card(CardRank.Nine, CardSuit.Clubs));
        var logic = provider.GetRequiredService<IDealerLogic>();
        var hand = MakeHand(new Card(CardRank.Eight, CardSuit.Spades), new Card(CardRank.Six, CardSuit.Hearts)); // 14
        var result = await logic.PlayAsync(hand);
        Assert.True(result.FinalEvaluation.IsBust);
    }
}
