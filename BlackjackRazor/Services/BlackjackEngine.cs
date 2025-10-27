using System;
using System.Linq;
using System.Threading.Tasks;
using BlackjackRazor.Models;
using BlackjackRazor.Services;

namespace BlackjackRazor.Services;

public class BlackjackEngine
{
    private readonly DeckApiClient _deckApi;
    public BlackjackEngine(DeckApiClient api) { _deckApi = api; }

    public async Task InitializeAsync(GameState state)
    {
        state.DeckId = await _deckApi.NewDeckAsync(state.DeckCount);
        state.Hands.Clear();
        state.Hands.Add(new PlayerHand { Bet = state.DefaultBet });
        state.ActiveHandIndex = 0;
        state.Dealer.Cards.Clear();
        await DealInitialAsync(state);
    }

    private async Task DealInitialAsync(GameState state)
    {
        var draw = await _deckApi.DrawAsync(state.DeckId, 4);
        // Player card 1, Dealer up, Player card 2, Dealer hole
        var p = state.Hands[0];
        p.Cards.Add(ToModel(draw[0]));
        state.Dealer.Cards.Add(ToModel(draw[1]));
        p.Cards.Add(ToModel(draw[2]));
        var hole = ToModel(draw[3]);
        state.Dealer.Cards.Add(new CardModel(hole.Code, hole.ImageUrl, hole.Value, FaceDown: true));
    }

    public bool CanHit(GameState s) => !s.GameResolved && !s.DealerPlaying && !s.ActiveHand.IsComplete && s.ActiveHand.HandValue() < 21;
    public bool CanStand(GameState s) => !s.GameResolved && !s.DealerPlaying && !s.ActiveHand.IsComplete;
    public bool CanDouble(GameState s) => CanHit(s) && s.ActiveHand.Cards.Count == 2 && !s.ActiveHand.DoubledDown;
    public bool CanSplit(GameState s)
        => !s.GameResolved && !s.DealerPlaying && s.Hands.Count == 1 && s.ActiveHand.Cards.Count == 2 && Rank(s.ActiveHand.Cards[0]) == Rank(s.ActiveHand.Cards[1]);

    private string Rank(CardModel c) => c.Code.Length >= 2 ? c.Code[..^1] : c.Code;

    public async Task HitAsync(GameState state)
    {
        if (!CanHit(state)) return;
        var cards = await _deckApi.DrawAsync(state.DeckId, 1);
        state.ActiveHand.Cards.Add(ToModel(cards[0]));
        if (state.ActiveHand.HandValue() >= 21) state.ActiveHand.IsComplete = true;
        await MaybeAdvanceAsync(state);
    }

    public async Task StandAsync(GameState state)
    {
        if (!CanStand(state)) return;
        state.ActiveHand.IsComplete = true;
        await MaybeAdvanceAsync(state);
    }

    public async Task DoubleDownAsync(GameState state)
    {
        if (!CanDouble(state)) return;
        state.ActiveHand.DoubledDown = true;
        state.ActiveHand.Bet *= 2;
        await HitAsync(state);
        state.ActiveHand.IsComplete = true;
        await MaybeAdvanceAsync(state);
    }

    public async Task SplitAsync(GameState state)
    {
        if (!CanSplit(state)) return;
        var original = state.Hands[0];
        var second = new PlayerHand { Bet = original.Bet, IsSplitHand = true };
        var firstCard = original.Cards[0];
        var secondCard = original.Cards[1];
        original.Cards.Clear();
        original.Cards.Add(firstCard);
        second.Cards.Add(secondCard);
        state.Hands.Add(second);
        // draw one card to each
        var draw = await _deckApi.DrawAsync(state.DeckId, 2);
        original.Cards.Add(ToModel(draw[0]));
        second.Cards.Add(ToModel(draw[1]));
    }

    private async Task MaybeAdvanceAsync(GameState state)
    {
        if (state.Hands.Any(h => !h.IsComplete))
        {
            if (state.ActiveHand.IsComplete)
            {
                var nextIndex = state.Hands.FindIndex(h => !h.IsComplete);
                if (nextIndex >= 0) state.ActiveHandIndex = nextIndex;
            }
            return;
        }
        // Reveal dealer & play
        state.Dealer.RevealHole = true;
        state.DealerPlaying = true;
        await DealerPlayAsync(state);
        Evaluate(state);
    }

    private async Task DealerPlayAsync(GameState state)
    {
        while (true)
        {
            var value = state.Dealer.HandValue();
            bool soft = state.Dealer.Cards.Any(c => c.Code.StartsWith("A")) && value <= 17;
            if (value < 17 || (soft && value < 18 && value < 17))
            {
                var draw = await _deckApi.DrawAsync(state.DeckId, 1);
                state.Dealer.Cards.Add(ToModel(draw[0]));
            }
            else break;
        }
        state.DealerPlaying = false;
    }

    private void Evaluate(GameState state)
    {
        decimal roundNet = 0m;
        foreach (var hand in state.Hands)
        {
            var playerTotal = hand.HandValue();
            var dealerTotal = state.Dealer.HandValue();
            if (playerTotal > 21)
            {
                hand.Outcome = "Bust"; state.Bankroll -= hand.Bet; roundNet -= hand.Bet; continue;
            }
            if (hand.IsBlackjack && !state.Dealer.IsBlackjack)
            {
                hand.Outcome = "Blackjack"; var win = hand.Bet * 1.5m; state.Bankroll += win; roundNet += win; continue;
            }
            if (dealerTotal > 21)
            {
                hand.Outcome = "Win"; state.Bankroll += hand.Bet; roundNet += hand.Bet; continue;
            }
            if (playerTotal > dealerTotal)
            {
                hand.Outcome = "Win"; state.Bankroll += hand.Bet; roundNet += hand.Bet;
            }
            else if (playerTotal < dealerTotal)
            {
                hand.Outcome = "Lose"; state.Bankroll -= hand.Bet; roundNet -= hand.Bet;
            }
            else
            {
                hand.Outcome = "Push"; // bankroll unchanged
            }
        }
        state.GameResolved = true;
        state.RoundNet = roundNet;
    }

    private CardModel ToModel(DeckApiClient.CardDto dto) => new(dto.Code, dto.Image, dto.NumericValue);
}
