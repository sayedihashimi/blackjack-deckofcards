using BlackjackRazor.Infrastructure;
using Microsoft.Extensions.Options;

namespace BlackjackRazor.Domain;

public interface IDealerLogic
{
    /// <summary>
    /// Plays out the dealer hand (drawing cards as needed) according to Blackjack rules and configuration.
    /// Returns a transcript of totals after each draw including initial state.
    /// </summary>
    Task<DealerPlayResult> PlayAsync(Hand dealerHand, CancellationToken ct = default);
}

public sealed record DealerPlayStep(int StepNumber, int Total, bool IsSoft, bool DrewCard, Card? CardDrawn);
public sealed record DealerPlayResult(IReadOnlyList<DealerPlayStep> Steps, HandEvaluation FinalEvaluation);

public sealed class DealerLogic : IDealerLogic
{
    private readonly IShoeManager _shoe;
    private readonly IOptionsMonitor<BlackjackOptions> _options;

    public DealerLogic(IShoeManager shoe, IOptionsMonitor<BlackjackOptions> options)
    {
        _shoe = shoe;
        _options = options;
    }

    public async Task<DealerPlayResult> PlayAsync(Hand dealerHand, CancellationToken ct = default)
    {
        var steps = new List<DealerPlayStep>();
        int step = 0;
        HandEvaluation eval = HandEvaluator.Evaluate(dealerHand);
        steps.Add(new DealerPlayStep(step, eval.Total, eval.IsSoft, false, null));

        // If dealer has natural blackjack (should only happen on initial two cards) stop immediately.
        if (eval.IsBlackjack)
            return new DealerPlayResult(steps, eval);

        bool hitSoft17 = _options.CurrentValue.DealerHitSoft17;

        while (true)
        {
            // Dealer stands on totals > 17 always; stands on hard 17; hits soft 17 if config says so.
            if (eval.IsBust) break;
            if (eval.Total > 17) break;
            if (eval.Total == 17 && (!eval.IsSoft || !hitSoft17)) break;

            // Draw one card
            var drawn = await _shoe.DrawAsync(1, ct);
            if (drawn.Count == 0) break; // shoe exhausted unexpectedly
            dealerHand.AddCard(drawn[0]);
            step++;
            eval = HandEvaluator.Evaluate(dealerHand);
            steps.Add(new DealerPlayStep(step, eval.Total, eval.IsSoft, true, drawn[0]));
        }

            dealerHand.MarkCompleted();
        return new DealerPlayResult(steps, eval);
    }

    // Removed placeholder method; DealerHitSoft17 now part of BlackjackOptions binding.
}
