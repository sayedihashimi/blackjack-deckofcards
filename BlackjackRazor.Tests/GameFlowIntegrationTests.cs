using System.Threading.Tasks;
using Xunit;
using BlackjackRazor.Domain;
using BlackjackRazor.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using BlackjackRazor.Data;

namespace BlackjackRazor.Tests;

// Deterministic shoe for integration scenarios
public sealed class IntegrationSequenceShoe : IShoeManager
{
    private readonly Queue<Card> _queue;
    public IntegrationSequenceShoe(IEnumerable<Card> cards) => _queue = new Queue<Card>(cards);
    public int Remaining => _queue.Count;
    public Task<IReadOnlyList<Card>> DrawAsync(int count, System.Threading.CancellationToken ct = default)
    {
        var list = new List<Card>(count);
        for (int i = 0; i < count && _queue.Count > 0; i++) list.Add(_queue.Dequeue());
        return Task.FromResult<IReadOnlyList<Card>>(list);
    }
}

public class GameFlowIntegrationTests
{
    private ServiceProvider Build(params Card[] shoeCards)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IShoeManager>(new IntegrationSequenceShoe(shoeCards));
        services.AddSingleton<IDealerLogic, DealerLogic>();
        services.AddSingleton<IPayoutService, PayoutService>();
        services.AddSingleton<IGameService, GameService>();
        services.Configure<BlackjackOptions>(o => { o.DefaultDeckCount = 1; o.DealerHitSoft17 = false; });
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task SplitAndDoubleFlow_ProducesTwoSettlementResultsAndExpectedNet()
    {
        // Card queue (draw order): P1, D1, P2, D2, Split draw to hand1, Split draw to hand2, Double draw (second hand), Dealer draws
        // Scenario: Initial 8,8 vs dealer 5 showing (dealer hole 6). Split -> hand1 8+2=10 stand (loss vs dealer 17), hand2 8+3 then Double draws K=21 (win 2*bet). Dealer draws 6 -> total 17 stands.
        // Net: (-bet) + (+2*bet) = +bet.
        var provider = Build(
            new Card(CardRank.Eight, CardSuit.Spades), // P1
            new Card(CardRank.Five, CardSuit.Hearts),  // D1
            new Card(CardRank.Eight, CardSuit.Clubs),  // P2
            new Card(CardRank.Six, CardSuit.Diamonds), // D2 -> dealer total 11
            new Card(CardRank.Two, CardSuit.Hearts),   // split draw -> hand1 second card (10)
            new Card(CardRank.Three, CardSuit.Clubs),  // split draw -> hand2 second card (11)
            new Card(CardRank.King, CardSuit.Spades),  // double draw for hand2 -> 21
            new Card(CardRank.Six, CardSuit.Hearts)    // dealer draw -> 17 stand
        );
        var game = provider.GetRequiredService<IGameService>();
        const decimal starting = 500m;
        const decimal bet = 20m;
        game.NewGame(starting, bet);
        await game.DealInitialAsync();
        await game.SplitAsync(); // creates two hands, active index at first split hand
        game.Stand();            // complete first hand (10 stands, likely losing later)
        var afterFirstStand = game.Refresh();
        Assert.Equal(1, afterFirstStand.ActiveHandIndex); // second hand now active
        await game.DoubleAsync(); // second hand doubles and completes
        var afterDouble = game.Refresh();
        Assert.True(afterDouble.PlayerHands[1].IsCompleted);
        Assert.Equal(2, afterDouble.PlayerHands.Count);
        await game.AdvanceDealerAsync();
        var settled = game.SettleRound();
        Assert.Equal(starting + bet, settled.Bankroll); // +20 net
        Assert.Equal(2, settled.SettlementResults.Count);
        Assert.Equal(bet, settled.RoundNetDelta);
        // Verify events contain sequence markers
        Assert.Contains(settled.Events, e => e == "Split pair");
        Assert.Contains(settled.Events, e => e == "Stand");
        Assert.Contains(settled.Events, e => e == "Double down");
        Assert.Contains(settled.Events, e => e.StartsWith("Round settled"));
    }

    [Fact]
    public async Task PushSequence_LeavesBankrollUnchangedAndRecordsSettlementEvent()
    {
        // Player: Ten + King = 20. Dealer: Nine + Ace = soft 20 -> stand.
        var provider = Build(
            new Card(CardRank.Ten, CardSuit.Spades),  // P1
            new Card(CardRank.Nine, CardSuit.Clubs),  // D1
            new Card(CardRank.King, CardSuit.Hearts), // P2 -> player 20
            new Card(CardRank.Ace, CardSuit.Diamonds) // D2 -> dealer soft 20
        );
        var game = provider.GetRequiredService<IGameService>();
        const decimal starting = 300m;
        const decimal bet = 30m;
        game.NewGame(starting, bet);
        await game.DealInitialAsync();
        game.Stand();
        await game.AdvanceDealerAsync(); // dealer stands on 20
        var settled = game.SettleRound();
        Assert.Equal(starting, settled.Bankroll); // push => no change
        Assert.Single(settled.SettlementResults);
        Assert.Equal(0m, settled.RoundNetDelta);
        Assert.Contains(settled.Events, e => e.StartsWith("Round settled"));
    }

    [Fact]
    public async Task BlackjackVsDealerNonBlackjack_AwardsThreeToTwoPayout()
    {
        // Player natural blackjack; dealer ends with 20 (not blackjack) -> +1.5*bet
        var provider = Build(
            new Card(CardRank.Ace, CardSuit.Spades),  // P1
            new Card(CardRank.Nine, CardSuit.Clubs),  // D1
            new Card(CardRank.King, CardSuit.Hearts), // P2 -> player blackjack
            new Card(CardRank.Ten, CardSuit.Diamonds) // D2 -> dealer 19 or 19? Actually 9+10=19 stands
        );
        var game = provider.GetRequiredService<IGameService>();
        const decimal starting = 1000m;
        const decimal bet = 40m;
        game.NewGame(starting, bet);
        await game.DealInitialAsync();
        // Player hand auto-completed
        await game.AdvanceDealerAsync();
        var settled = game.SettleRound();
        // 3:2 payout: net delta +60 (bet*1.5)
        Assert.Equal(starting + bet * 1.5m, settled.Bankroll);
        Assert.Equal(bet * 1.5m, settled.RoundNetDelta);
        Assert.Contains(settled.Events, e => e.Contains("blackjack", System.StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task DealerBustAfterPlayerStand_RecordsDealerBustAndPositiveNet()
    {
        // Guaranteed dealer bust scenario:
        // Player: 10 + 2 = 12 (stands).
        // Dealer: 9 + 6 = 15 (must hit) then draws 7 -> 22 bust.
        var provider = Build(
            new Card(CardRank.Ten, CardSuit.Spades),   // P1
            new Card(CardRank.Nine, CardSuit.Clubs),   // D1 (dealer 9)
            new Card(CardRank.Two, CardSuit.Hearts),   // P2 (player total 12)
            new Card(CardRank.Six, CardSuit.Diamonds), // D2 (dealer total 15 must hit)
            new Card(CardRank.Seven, CardSuit.Spades)  // dealer hit -> 22 bust
        );
        var game = provider.GetRequiredService<IGameService>();
        const decimal starting = 250m;
        const decimal bet = 25m;
        game.NewGame(starting, bet);
        await game.DealInitialAsync();
        game.Stand(); // player completes with 12
        var dealerPhase = await game.AdvanceDealerAsync();
        var lastEvent = dealerPhase.Events[^1];
        Assert.StartsWith("Dealer bust", lastEvent);
        var settled = game.SettleRound();
        Assert.Equal(starting + bet, settled.Bankroll); // player wins despite low total due to dealer bust
        Assert.Equal(bet, settled.RoundNetDelta);
        Assert.Single(settled.SettlementResults);
        Assert.Equal(HandOutcome.Win, settled.SettlementResults[0].Outcome);
        Assert.Contains(settled.Events, e => e.StartsWith("Round settled"));
    }
}
