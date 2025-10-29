namespace BlackjackRazor.Domain;

public sealed record HandEvaluation(int Total, bool IsSoft, bool IsBlackjack, bool IsBust, IReadOnlyList<int> AllTotals)
{
    public override string ToString() => $"Total={Total} Soft={IsSoft} BJ={IsBlackjack} Bust={IsBust}";
}

public static class HandEvaluator
{
    public static HandEvaluation Evaluate(Hand hand)
    {
        var cards = hand.Cards;
        int aceCount = cards.Count(c => c.IsAce);
        int nonAceSum = cards.Where(c => !c.IsAce).Sum(c => c.BaseValue);

        var possibleTotals = new List<int>();
        // treat all aces as 1 initially
        int baseTotal = nonAceSum + aceCount * 1;
        possibleTotals.Add(baseTotal);
        if (aceCount > 0)
        {
            // Try promoting one ace to 11 (i.e., +10) if it does not bust
            int withOneAceHigh = baseTotal + 10;
            if (withOneAceHigh <= 21)
                possibleTotals.Add(withOneAceHigh);
        }
        // No need to consider more than one 11-valued ace; two 11s would exceed 21 except for very small nonAceSum.

        int best = possibleTotals.Max();
        bool isSoft = aceCount > 0 && best != baseTotal; // we successfully used an ace as 11
        bool isBust = baseTotal > 21 && possibleTotals.All(t => t > 21);

        bool isBlackjack = cards.Count == 2 &&
                           ((aceCount == 1 && cards.Any(c => c.BaseValue == 10)));

        // If all totals > 21, choose lowest (still bust but report canonical) else highest <=21.
        if (possibleTotals.All(t => t > 21))
        {
            best = possibleTotals.Min();
        }

        return new HandEvaluation(best, isSoft, isBlackjack, isBust, possibleTotals.OrderBy(t => t).ToList());
    }
}
