using System.Collections.Generic;
using BlackjackRazor.Domain;
using BlackjackRazor.Data;
using Xunit;

namespace BlackjackRazor.Tests;

public class PayoutServiceTests
{
    private readonly IPayoutService _svc = new PayoutService();

    private static HandEvaluation Eval(int total, bool soft=false, bool bj=false, bool bust=false) => new(total, soft, bj, bust, new List<int>{total});

    [Fact]
    public void PlayerBlackjackVsDealerNonBlackjack_WinsThreeToTwo()
    {
        var player = Eval(21, soft:true, bj:true);
        var dealer = Eval(20);
        var result = _svc.Compute(player, dealer, 100m, wasSplit:false);
        Assert.Equal(HandOutcome.Blackjack, result.Outcome);
        Assert.Equal(150m, result.NetDelta);
    }

    [Fact]
    public void SplitTwentyOne_EvenMoneyWin()
    {
        var player = Eval(21, soft:true, bj:true); // evaluator flags BJ, but wasSplit prevents natural payout
        var dealer = Eval(18);
        var result = _svc.Compute(player, dealer, 50m, wasSplit:true);
        Assert.Equal(HandOutcome.Win, result.Outcome);
        Assert.Equal(50m, result.NetDelta);
    }

    [Fact]
    public void PushTotals_ReturnsZero()
    {
        var player = Eval(18);
        var dealer = Eval(18);
        var result = _svc.Compute(player, dealer, 25m, wasSplit:false);
        Assert.Equal(HandOutcome.Push, result.Outcome);
        Assert.Equal(0m, result.NetDelta);
    }

    [Fact]
    public void DealerBust_PlayerWinsEvenMoney()
    {
        var player = Eval(17);
        var dealer = Eval(23, bust:true);
        var result = _svc.Compute(player, dealer, 40m, wasSplit:false);
        Assert.Equal(HandOutcome.Win, result.Outcome);
        Assert.Equal(40m, result.NetDelta);
    }

    [Fact]
    public void PlayerBust_LosesBet()
    {
        var player = Eval(25, bust:true);
        var dealer = Eval(19);
        var result = _svc.Compute(player, dealer, 30m, wasSplit:false);
        Assert.Equal(HandOutcome.Bust, result.Outcome);
        Assert.Equal(-30m, result.NetDelta);
    }

    [Fact]
    public void DealerBlackjack_PlayerNonBlackjack_Lose()
    {
        var player = Eval(21); // 3-card 21 not blackjack
        var dealer = Eval(21, bj:true);
        var result = _svc.Compute(player, dealer, 20m, wasSplit:false);
        Assert.Equal(HandOutcome.Lose, result.Outcome);
        Assert.Equal(-20m, result.NetDelta);
    }
}
