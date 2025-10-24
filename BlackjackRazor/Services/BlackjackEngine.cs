using BlackjackRazor.Services;

namespace BlackjackRazor.Engine;

public enum HandResult { Pending, Blackjack, Win, Lose, Push, Bust }
public enum ActionType { Deal, Hit, Stand, Split, DoubleDown }

public class PlayerHand
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public List<DeckApiClient.Card> Cards { get; set; } = new();
    public int Bet { get; set; }
    public bool IsSettled { get; set; }
    public bool DoubledDown { get; set; }
    public HandResult Result { get; set; } = HandResult.Pending;
}

public class GameState
{
    public string DeckId { get; set; } = string.Empty;
    public List<DeckApiClient.Card> DealerCards { get; set; } = new();
    public List<PlayerHand> PlayerHands { get; set; } = new();
    public int ActiveHandIndex { get; set; } = 0;
    public int ShoeRemaining { get; set; }
    public bool DealerDone { get; set; }
    public int DefaultBet { get; set; } = 10;
    public bool HandInProgress { get; set; }
    public int DeckCount { get; set; } = 6;
}

public record HandValue(int Total, bool IsSoft, bool IsBlackjack);

public static class BlackjackEngine
{
    private static readonly Dictionary<string, int> RankValues = new()
    {
        ["ACE"] = 11,
        ["KING"] = 10,
        ["QUEEN"] = 10,
        ["JACK"] = 10,
        ["10"] = 10,
        ["9"] = 9,
        ["8"] = 8,
        ["7"] = 7,
        ["6"] = 6,
        ["5"] = 5,
        ["4"] = 4,
        ["3"] = 3,
        ["2"] = 2
    };

    public static HandValue Evaluate(IEnumerable<DeckApiClient.Card> cards)
    {
        int total = 0; int acesHigh = 0; // acesHigh counts aces currently valued at 11
        foreach (var c in cards)
        {
            if (c.Value == "ACE")
            {
                acesHigh++;
                total += 11;
            }
            else
            {
                total += RankValues[c.Value];
            }
        }
        // Downgrade aces from 11 to 1 while busting
        int acesHighWorking = acesHigh;
        while (total > 21 && acesHighWorking > 0)
        {
            total -= 10; // convert one ace to value 1
            acesHighWorking--;
        }
        bool soft = acesHighWorking > 0; // still at least one ace counted as 11
        bool blackjack = cards.Count() == 2 && total == 21;
        return new HandValue(total, soft, blackjack);
    }

    public static bool CanSplit(PlayerHand hand) => hand.Cards.Count == 2 && hand.Cards[0].Value == hand.Cards[1].Value;
    public static bool CanDouble(PlayerHand hand) => hand.Cards.Count == 2 && !hand.DoubledDown;
    public static bool IsBust(HandValue hv) => hv.Total > 21;

    public static int Payout(PlayerHand hand, HandValue hv, HandValue dealerHv)
    {
        if (hand.Result == HandResult.Blackjack) return (int)(hand.Bet * 2.5); // 3:2 plus original bet (net +1.5)
        if (hand.Result == HandResult.Push) return hand.Bet; // return bet
        if (hand.Result == HandResult.Win) return hand.Bet * 2; // 1:1 plus original
        return 0; // Lose or Bust
    }

    public static void ResolveHand(PlayerHand hand, HandValue hv, HandValue dealerHv)
    {
        if (hv.IsBlackjack && hand.Cards.Count == 2)
        {
            hand.Result = HandResult.Blackjack;
            hand.IsSettled = true;
            return;
        }
        if (hv.Total > 21)
        {
            hand.Result = HandResult.Bust;
            hand.IsSettled = true;
            return;
        }
        if (dealerHv.Total > 21)
        {
            hand.Result = HandResult.Win;
        }
        else if (hv.Total > dealerHv.Total)
        {
            hand.Result = HandResult.Win;
        }
        else if (hv.Total < dealerHv.Total)
        {
            hand.Result = HandResult.Lose;
        }
        else
        {
            hand.Result = HandResult.Push;
        }
        hand.IsSettled = true;
    }

    public static IEnumerable<ActionType> ValidActions(GameState state)
    {
        if (!state.HandInProgress) yield break;
        var hand = state.PlayerHands[state.ActiveHandIndex];
        var hv = Evaluate(hand.Cards);
        if (hv.Total >= 21) yield break;
        yield return ActionType.Hit;
        yield return ActionType.Stand;
        if (CanSplit(hand)) yield return ActionType.Split;
        if (CanDouble(hand)) yield return ActionType.DoubleDown;
    }

    public static async Task DealAsync(GameState state, DeckApiClient api, int bet, Func<Task<List<DeckApiClient.Card>>>? preloaded = null)
    {
        if (state.HandInProgress) return;
        state.PlayerHands.Clear();
        state.DealerCards.Clear();
        state.DealerDone = false;
        var playerHand = new PlayerHand { Bet = bet };
        var cardsNeeded = 4; // two player, two dealer
        var drawn = await api.DrawAsync(state.DeckId, cardsNeeded);
        // order: player, dealer, player, dealer
        playerHand.Cards.Add(drawn[0]);
        state.DealerCards.Add(drawn[1]);
        playerHand.Cards.Add(drawn[2]);
        state.DealerCards.Add(drawn[3]);
        state.PlayerHands.Add(playerHand);
        state.HandInProgress = true;
        state.ActiveHandIndex = 0;
        var hv = Evaluate(playerHand.Cards);
        if (hv.IsBlackjack)
        {
            // dealer peek: if dealer also blackjack -> push resolved later
            await DealerPlayAsync(state, api);
            FinalizeRound(state);
        }
    }

    public static async Task HitAsync(GameState state, DeckApiClient api)
    {
        if (!state.HandInProgress) return;
        var hand = state.PlayerHands[state.ActiveHandIndex];
        var card = (await api.DrawAsync(state.DeckId, 1))[0];
        hand.Cards.Add(card);
        var hv = Evaluate(hand.Cards);
        if (hv.Total >= 21)
        {
            // auto stand / move
            if (hv.Total > 21) hand.Result = HandResult.Bust;
            await AdvanceOrDealerAsync(state, api);
        }
    }

    public static async Task StandAsync(GameState state, DeckApiClient api)
    {
        if (!state.HandInProgress) return;
        await AdvanceOrDealerAsync(state, api);
    }

    public static async Task DoubleDownAsync(GameState state, DeckApiClient api)
    {
        var hand = state.PlayerHands[state.ActiveHandIndex];
        if (!CanDouble(hand)) return;
        hand.Bet *= 2;
        hand.DoubledDown = true;
        var card = (await api.DrawAsync(state.DeckId, 1))[0];
        hand.Cards.Add(card);
        var hv = Evaluate(hand.Cards);
        if (hv.Total > 21) hand.Result = HandResult.Bust;
        await AdvanceOrDealerAsync(state, api);
    }

    public static async Task SplitAsync(GameState state, DeckApiClient api)
    {
        var hand = state.PlayerHands[state.ActiveHandIndex];
        if (!CanSplit(hand)) return;
        var second = new PlayerHand { Bet = hand.Bet };
        second.Cards.Add(hand.Cards[1]);
        hand.Cards.RemoveAt(1);
        // draw one card for each split hand
        var draw = await api.DrawAsync(state.DeckId, 2);
        hand.Cards.Add(draw[0]);
        second.Cards.Add(draw[1]);
        state.PlayerHands.Insert(state.ActiveHandIndex + 1, second);
    }

    private static async Task AdvanceOrDealerAsync(GameState state, DeckApiClient api)
    {
        // move to next unsettled hand
        while (true)
        {
            if (state.ActiveHandIndex < state.PlayerHands.Count)
            {
                var h = state.PlayerHands[state.ActiveHandIndex];
                var hv = Evaluate(h.Cards);
                if (hv.Total > 21 || hv.IsBlackjack || h.Result == HandResult.Bust)
                {
                    h.IsSettled = true;
                    state.ActiveHandIndex++;
                    continue;
                }
                // this hand still playable
                return;
            }
            break;
        }
        // all player hands done
        await DealerPlayAsync(state, api);
        FinalizeRound(state);
    }

    public static async Task DealerPlayAsync(GameState state, DeckApiClient api)
    {
        if (state.DealerDone) return;
        var dealerHv = Evaluate(state.DealerCards);
        // hit soft 17 rule: continue while total <17 OR (total==17 and soft)
        while (dealerHv.Total < 17 || (dealerHv.Total == 17 && dealerHv.IsSoft))
        {
            var card = (await api.DrawAsync(state.DeckId, 1))[0];
            state.DealerCards.Add(card);
            dealerHv = Evaluate(state.DealerCards);
        }
        state.DealerDone = true;
    }

    private static void FinalizeRound(GameState state)
    {
        var dealerHv = Evaluate(state.DealerCards);
        foreach (var hand in state.PlayerHands)
        {
            if (hand.IsSettled && hand.Result != HandResult.Pending) continue;
            var hv = Evaluate(hand.Cards);
            ResolveHand(hand, hv, dealerHv);
        }
        state.HandInProgress = false;
    }
}
