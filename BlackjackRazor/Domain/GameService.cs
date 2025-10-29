using BlackjackRazor.Infrastructure;
using BlackjackRazor.Data;

namespace BlackjackRazor.Domain;

public enum GamePhase
{
    NotStarted,
    PlayerActing,
    DealerActing,
    Settled
}

public sealed class GameContext
{
    public List<Hand> PlayerHands { get; } = new();
    public Hand DealerHand { get; set; } = new();
    public int ActiveHandIndex { get; set; } = 0;
    public decimal Bankroll { get; set; }
    public decimal CurrentBet { get; set; }
    public GamePhase Phase { get; set; } = GamePhase.NotStarted;
    public bool DealerPlayed { get; set; }
    public List<DealerPlayStep> DealerTranscript { get; } = new();
    public List<string> Events { get; } = new();
    public List<SettlementHandResult> SettlementResults { get; } = new();
    public decimal LastRoundNet { get; set; }
    public int? GameId { get; set; }
}

public sealed record HandSnapshot(IReadOnlyList<Card> Cards, HandEvaluation Evaluation, bool IsCompleted, bool WasSplitChild, bool HasDoubled);
public sealed record SettlementHandResult(int HandIndex, HandOutcome Outcome, decimal Bet, decimal Payout, decimal NetDelta);
public sealed record GameStateSnapshot(
    GamePhase Phase,
    decimal Bankroll,
    decimal CurrentBet,
    int ActiveHandIndex,
    IReadOnlyList<HandSnapshot> PlayerHands,
    HandSnapshot Dealer,
    bool DealerPlayed,
    IReadOnlyList<string> Events,
    IReadOnlyList<SettlementHandResult> SettlementResults,
    decimal RoundNetDelta,
    int? GameId
);

public interface IGameService
{
    GameStateSnapshot NewGame(decimal startingBankroll, decimal bet);
    Task<GameStateSnapshot> DealInitialAsync(CancellationToken ct = default);
    Task<GameStateSnapshot> HitAsync(CancellationToken ct = default);
    GameStateSnapshot Stand();
    Task<GameStateSnapshot> AdvanceDealerAsync(CancellationToken ct = default);
    GameStateSnapshot SettleRound();
    Task<GameStateSnapshot> SplitAsync(CancellationToken ct = default);
    Task<GameStateSnapshot> DoubleAsync(CancellationToken ct = default);
    // Rehydrate internal context from a previously saved snapshot (session persistence round-trip)
    void Load(GameStateSnapshot snapshot);
    GameContext RawContext { get; }
    void SetGameId(int gameId);
    GameStateSnapshot Refresh();
}

public sealed class GameService : IGameService
{
    private readonly IShoeManager _shoe;
    private readonly IPayoutService _payout;
    private readonly IDealerLogic _dealer;
    private readonly GameContext _ctx = new();

    public GameContext RawContext => _ctx;

    public GameService(IShoeManager shoe, IPayoutService payout, IDealerLogic dealer)
    {
        _shoe = shoe;
        _payout = payout;
        _dealer = dealer;
    }

    public GameStateSnapshot NewGame(decimal startingBankroll, decimal bet)
    {
        _ctx.PlayerHands.Clear();
        _ctx.DealerHand = new Hand();
        _ctx.PlayerHands.Add(new Hand());
        _ctx.ActiveHandIndex = 0;
        _ctx.Bankroll = startingBankroll;
        _ctx.CurrentBet = bet;
        _ctx.Phase = GamePhase.NotStarted; // must call DealInitial
        _ctx.DealerPlayed = false;
        _ctx.Events.Clear();
        _ctx.Events.Add("New game started");
        _ctx.GameId = null; // will be attached after persistence
        return Snapshot();
    }

    public void Load(GameStateSnapshot snapshot)
    {
        if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
        _ctx.PlayerHands.Clear();
        foreach (var hs in snapshot.PlayerHands)
        {
            var hand = new Hand { WasSplitChild = hs.WasSplitChild };
            foreach (var c in hs.Cards)
            {
                hand.AddCard(c);
            }
            if (hs.HasDoubled) hand.MarkDoubled();
            if (hs.IsCompleted) hand.MarkCompleted();
            _ctx.PlayerHands.Add(hand);
        }
        // Dealer hand
        var dealer = new Hand();
        foreach (var c in snapshot.Dealer.Cards)
        {
            dealer.AddCard(c);
        }
        if (snapshot.Dealer.IsCompleted) dealer.MarkCompleted();
        _ctx.DealerHand = dealer;
        _ctx.ActiveHandIndex = snapshot.ActiveHandIndex;
        _ctx.Bankroll = snapshot.Bankroll;
        _ctx.CurrentBet = snapshot.CurrentBet;
        _ctx.Phase = snapshot.Phase;
        _ctx.DealerPlayed = snapshot.DealerPlayed;
        _ctx.Events.Clear();
        foreach (var e in snapshot.Events) _ctx.Events.Add(e);
        _ctx.SettlementResults.Clear();
        foreach (var r in snapshot.SettlementResults) _ctx.SettlementResults.Add(r);
        _ctx.LastRoundNet = snapshot.RoundNetDelta;
        _ctx.GameId = snapshot.GameId;
    }

    public async Task<GameStateSnapshot> DealInitialAsync(CancellationToken ct = default)
    {
        RequirePhase(GamePhase.NotStarted);
        var player = _ctx.PlayerHands[0];
        var initial = await _shoe.DrawAsync(4, ct); // 2 player, 2 dealer
        player.AddCard(initial[0]);
        _ctx.DealerHand.AddCard(initial[1]);
        player.AddCard(initial[2]);
        _ctx.DealerHand.AddCard(initial[3]);
        // Natural blackjack handling: immediately mark completed so further actions (Hit/Stand/Double) are gated.
        var eval = HandEvaluator.Evaluate(player);
        if (eval.IsBlackjack)
        {
            player.MarkCompleted();
            _ctx.Events.Add("Player blackjack!");
        }
        _ctx.Phase = GamePhase.PlayerActing;
        // Check for natural blackjack immediate settlement trigger later (still allow snapshot now)
        return Snapshot();
    }

    public async Task<GameStateSnapshot> HitAsync(CancellationToken ct = default)
    {
        RequirePhase(GamePhase.PlayerActing);
        if (_ctx.ActiveHandIndex >= _ctx.PlayerHands.Count) return Snapshot();
        var hand = _ctx.PlayerHands[_ctx.ActiveHandIndex];
        if (hand.IsCompleted) return Snapshot();
        var draw = await _shoe.DrawAsync(1, ct);
        if (draw.Count > 0) hand.AddCard(draw[0]);
        var eval = HandEvaluator.Evaluate(hand);
        if (eval.IsBust || eval.IsBlackjack) hand.MarkCompleted();
        if (eval.IsBust) _ctx.Events.Add("Hand bust");
        else if (eval.IsBlackjack) _ctx.Events.Add("Hand blackjack");
        // Advance to next hand if completed and there are remaining
        AdvanceActiveHandIfNeeded();
        MaybeAutoSettleOnAllBust();
        return Snapshot();
    }

    public GameStateSnapshot Stand()
    {
        RequirePhase(GamePhase.PlayerActing);
        var hand = _ctx.PlayerHands[_ctx.ActiveHandIndex];
        hand.MarkCompleted();
        _ctx.Events.Add("Stand");
        AdvanceActiveHandIfNeeded();
        MaybeAutoSettleOnAllBust();
        return Snapshot();
    }

    public async Task<GameStateSnapshot> AdvanceDealerAsync(CancellationToken ct = default)
    {
        RequirePhase(GamePhase.PlayerActing);
        // Ensure all player hands completed first
        if (_ctx.PlayerHands.Any(h => !h.IsCompleted)) return Snapshot();
        _ctx.Phase = GamePhase.DealerActing;
        var playResult = await _dealer.PlayAsync(_ctx.DealerHand, ct);
        _ctx.DealerPlayed = true;
        _ctx.DealerTranscript.Clear();
        foreach (var s in playResult.Steps) _ctx.DealerTranscript.Add(s);
        _ctx.Phase = GamePhase.Settled; // will settle immediately after dealer for now
        var dealerEval = HandEvaluator.Evaluate(_ctx.DealerHand);
        if (dealerEval.IsBust) _ctx.Events.Add("Dealer bust");
        else _ctx.Events.Add($"Dealer stands on {dealerEval.Total}{(dealerEval.IsSoft?" (Soft)":"")}");
        return Snapshot();
    }

    public GameStateSnapshot SettleRound()
    {
        RequirePhase(GamePhase.Settled, GamePhase.DealerActing);
        var dealerEval = HandEvaluator.Evaluate(_ctx.DealerHand);
        decimal totalDelta = 0m;
        _ctx.SettlementResults.Clear();
        foreach (var hand in _ctx.PlayerHands)
        {
            var eval = HandEvaluator.Evaluate(hand);
            var wasSplit = hand.WasSplitChild;
            var bet = hand.HasDoubled ? _ctx.CurrentBet * 2 : _ctx.CurrentBet;
            var result = _payout.Compute(eval, dealerEval, bet, wasSplit);
            totalDelta += result.NetDelta;
            int idx = _ctx.PlayerHands.IndexOf(hand);
            _ctx.SettlementResults.Add(new SettlementHandResult(idx, result.Outcome, bet, result.PayoutAmount, result.NetDelta));
        }
        _ctx.Bankroll += totalDelta;
        _ctx.Events.Add($"Round settled (net {(totalDelta>=0?"+":"")}{totalDelta})");
        _ctx.LastRoundNet = totalDelta;
        return Snapshot();
    }

    public async Task<GameStateSnapshot> SplitAsync(CancellationToken ct = default)
    {
        RequirePhase(GamePhase.PlayerActing);
        var hand = _ctx.PlayerHands[_ctx.ActiveHandIndex];
        if (hand.Cards.Count != 2) return Snapshot();
        if (hand.Cards[0].Rank != hand.Cards[1].Rank) return Snapshot();
        // Create two hands
        var firstCard = hand.Cards[0];
        var secondCard = hand.Cards[1];
        hand.MarkCompleted(); // replace with new hands
        var h1 = new Hand { WasSplitChild = true };
        var h2 = new Hand { WasSplitChild = true };
        h1.AddCard(firstCard);
        h2.AddCard(secondCard);
        // Draw one card to each
        var draws = await _shoe.DrawAsync(2, ct);
        if (draws.Count > 0) h1.AddCard(draws[0]);
        if (draws.Count > 1) h2.AddCard(draws[1]);
        // Replace original hand list
        _ctx.PlayerHands.RemoveAt(_ctx.ActiveHandIndex);
        _ctx.PlayerHands.Insert(_ctx.ActiveHandIndex, h2);
        _ctx.PlayerHands.Insert(_ctx.ActiveHandIndex, h1);
        // Active hand index stays at first split hand
        _ctx.Events.Add("Split pair");
        return Snapshot();
    }

    public async Task<GameStateSnapshot> DoubleAsync(CancellationToken ct = default)
    {
        RequirePhase(GamePhase.PlayerActing);
        var hand = _ctx.PlayerHands[_ctx.ActiveHandIndex];
        if (hand.Cards.Count != 2 || hand.HasDoubled) return Snapshot();
        hand.MarkDoubled();
        var draw = await _shoe.DrawAsync(1, ct);
        if (draw.Count > 0) hand.AddCard(draw[0]);
        hand.MarkCompleted();
        _ctx.Events.Add("Double down");
        AdvanceActiveHandIfNeeded();
        MaybeAutoSettleOnAllBust();
        return Snapshot();
    }

    private void AdvanceActiveHandIfNeeded()
    {
        while (_ctx.ActiveHandIndex < _ctx.PlayerHands.Count && _ctx.PlayerHands[_ctx.ActiveHandIndex].IsCompleted)
        {
            _ctx.ActiveHandIndex++;
        }
        if (_ctx.ActiveHandIndex >= _ctx.PlayerHands.Count)
        {
            // All player hands done; ready for dealer
            _ctx.Phase = GamePhase.PlayerActing; // remains until dealer advance requested
        }
    }

    private void MaybeAutoSettleOnAllBust()
    {
        if (_ctx.PlayerHands.Count > 0 && _ctx.PlayerHands.All(h => h.IsCompleted) && _ctx.PlayerHands.All(h => HandEvaluator.Evaluate(h).IsBust))
        {
            if (_ctx.Phase != GamePhase.Settled)
            {
                _ctx.Phase = GamePhase.Settled;
                _ctx.Events.Add("All player hands bust – round settled");
                // Perform settlement (losses only); dealer not played, keep hole card hidden.
                var dealerEval = HandEvaluator.Evaluate(_ctx.DealerHand); // not used for bust payouts but passed for consistency
                decimal totalDelta = 0m;
                _ctx.SettlementResults.Clear();
                foreach (var hand in _ctx.PlayerHands)
                {
                    var eval = HandEvaluator.Evaluate(hand);
                    var bet = hand.HasDoubled ? _ctx.CurrentBet * 2 : _ctx.CurrentBet;
                    // Bust outcome: net delta already accounted by bet removal; payout service can compute but we shortcut.
                    var outcome = HandOutcome.Bust;
                    decimal payout = 0m; // no return
                    decimal net = -bet; // total loss
                    totalDelta += net;
                    int idx = _ctx.PlayerHands.IndexOf(hand);
                    _ctx.SettlementResults.Add(new SettlementHandResult(idx, outcome, bet, payout, net));
                }
                _ctx.Bankroll += totalDelta; // apply net (negative)
                _ctx.LastRoundNet = totalDelta;
                _ctx.Events.Add($"Round settled (net {(totalDelta>=0?"+":"")}{totalDelta})");
            }
        }
    }

    private void RequirePhase(params GamePhase[] allowed)
    {
        if (!allowed.Contains(_ctx.Phase))
            throw new InvalidOperationException($"Operation not valid in phase {_ctx.Phase}");
    }

    private GameStateSnapshot Snapshot()
    {
        var playerSnaps = _ctx.PlayerHands.Select(h => new HandSnapshot(
            h.Cards,
            HandEvaluator.Evaluate(h),
            h.IsCompleted,
            h.WasSplitChild,
            h.HasDoubled
        )).ToList();
        var dealerSnap = new HandSnapshot(_ctx.DealerHand.Cards, HandEvaluator.Evaluate(_ctx.DealerHand), _ctx.DealerHand.IsCompleted, false, false);
        return new GameStateSnapshot(
            _ctx.Phase,
            _ctx.Bankroll,
            _ctx.CurrentBet,
            _ctx.ActiveHandIndex,
            playerSnaps,
            dealerSnap,
            _ctx.DealerPlayed,
            _ctx.Events.ToList(),
            _ctx.SettlementResults.ToList(),
            _ctx.LastRoundNet,
            _ctx.GameId
        );
    }

    // Attach a persisted GameId after creating DB row
    public void SetGameId(int gameId)
    {
        _ctx.GameId = gameId;
    }

    public GameStateSnapshot Refresh() => Snapshot();
}
