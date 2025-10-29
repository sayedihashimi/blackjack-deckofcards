using BlackjackRazor.Domain;
using Xunit;

namespace BlackjackRazor.Tests;

public class HandEvaluatorTests
{
    [Fact]
    public void Blackjack_AceTen_IsBlackjack()
    {
        var hand = new Hand();
        hand.AddCard(new Card(CardRank.Ace, CardSuit.Spades));
        hand.AddCard(new Card(CardRank.King, CardSuit.Hearts));
        var eval = HandEvaluator.Evaluate(hand);
        Assert.True(eval.IsBlackjack);
        Assert.Equal(21, eval.Total);
        Assert.True(eval.IsSoft);
    }

    [Theory]
    [InlineData(CardRank.Nine, 20)]
    [InlineData(CardRank.Eight, 19)]
    [InlineData(CardRank.Seven, 18)]
    public void SoftHands_ReportSoft(CardRank other, int expectedTotal)
    {
        var hand = new Hand();
        hand.AddCard(new Card(CardRank.Ace, CardSuit.Clubs));
        hand.AddCard(new Card(other, CardSuit.Diamonds));
        var eval = HandEvaluator.Evaluate(hand);
        Assert.Equal(expectedTotal, eval.Total);
        Assert.True(eval.IsSoft);
        Assert.False(eval.IsBust);
    }

    [Fact]
    public void MultipleAces_ChoosesBestUnder21()
    {
        var hand = new Hand();
        hand.AddCard(new Card(CardRank.Ace, CardSuit.Clubs));
        hand.AddCard(new Card(CardRank.Ace, CardSuit.Diamonds));
        hand.AddCard(new Card(CardRank.Nine, CardSuit.Spades));
        var eval = HandEvaluator.Evaluate(hand);
        // A,A,9 -> possible totals: (1+1+9)=11 and promote one ace: 21
        Assert.Equal(21, eval.Total);
        Assert.True(eval.IsSoft);
    }

    [Fact]
    public void BustHand_FlagsBust()
    {
        var hand = new Hand();
        hand.AddCard(new Card(CardRank.King, CardSuit.Spades));
        hand.AddCard(new Card(CardRank.Queen, CardSuit.Diamonds));
        hand.AddCard(new Card(CardRank.Nine, CardSuit.Hearts));
        var eval = HandEvaluator.Evaluate(hand);
        Assert.True(eval.IsBust);
    }
}
