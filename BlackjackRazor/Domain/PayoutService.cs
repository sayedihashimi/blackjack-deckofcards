using BlackjackRazor.Data;
namespace BlackjackRazor.Domain;

public interface IPayoutService
{
    /// <summary>
    /// Computes outcome and net delta for a player hand versus dealer.
    /// </summary>
    /// <param name="player">Evaluated player hand.</param>
    /// <param name="dealer">Evaluated dealer hand.</param>
    /// <param name="bet">Original bet amount (pre-double). If doubled, pass 2x bet externally.</param>
    /// <param name="wasSplit">True if this hand originated from a split (affects blackjack payout rules).</param>
    /// <returns>PayoutResult describing outcome and net gain/loss.</returns>
    PayoutResult Compute(HandEvaluation player, HandEvaluation dealer, decimal bet, bool wasSplit);
}

public sealed record PayoutResult(HandOutcome Outcome, decimal NetDelta, decimal PayoutAmount)
{
    public override string ToString() => $"{Outcome} Net={NetDelta} Paid={PayoutAmount}";
}

/// <summary>
/// Applies standard Blackjack payout rules (3:2 natural blackjack, even money otherwise).
/// Notes:
/// - A natural blackjack occurs only on an initial two-card, non-split hand with total 21.
/// - Split 21 (e.g. Ace + Ten after splitting aces) counts as normal 21 => even-money win.
/// - Bust always loses regardless of dealer bust.
/// - Push: equal totals (including both blackjack) yields zero net.
/// - Dealer blackjack vs player blackjack => push; dealer blackjack vs non-blackjack 21 loses for player.
/// </summary>
public sealed class PayoutService : IPayoutService
{
    public PayoutResult Compute(HandEvaluation player, HandEvaluation dealer, decimal bet, bool wasSplit)
    {
        // Player bust loses immediately.
        if (player.IsBust)
            return new PayoutResult(HandOutcome.Bust, -bet, 0m);

        // Dealer bust: player wins (blackjack rule already handled below if natural).
        if (dealer.IsBust)
        {
            if (IsNaturalBlackjack(player, wasSplit))
            {
                var payout = bet * 1.5m; // 3:2
                return new PayoutResult(HandOutcome.Blackjack, payout, payout);
            }
            return new PayoutResult(HandOutcome.Win, bet, bet);
        }

        // Natural blackjack scenarios.
        bool playerNatural = IsNaturalBlackjack(player, wasSplit);
        bool dealerNatural = dealer.IsBlackjack; // Dealer never split; two-card 21 indicates natural.

        if (playerNatural && dealerNatural)
            return new PayoutResult(HandOutcome.Push, 0m, 0m);
        if (dealerNatural && !playerNatural)
            return new PayoutResult(HandOutcome.Lose, -bet, 0m);
        if (playerNatural && !dealerNatural)
        {
            var payout = bet * 1.5m;
            return new PayoutResult(HandOutcome.Blackjack, payout, payout);
        }

        // Non-blackjack comparisons.
        if (player.Total > dealer.Total)
            return new PayoutResult(HandOutcome.Win, bet, bet);
        if (player.Total < dealer.Total)
            return new PayoutResult(HandOutcome.Lose, -bet, 0m);

        // Equal totals => push.
        return new PayoutResult(HandOutcome.Push, 0m, 0m);
    }

    private static bool IsNaturalBlackjack(HandEvaluation eval, bool wasSplit)
    {
        return eval.IsBlackjack && !wasSplit; // exclude split-origin 21
    }
}
