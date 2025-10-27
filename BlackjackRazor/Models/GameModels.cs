using System.Collections.Generic;
using System.Linq;

namespace BlackjackRazor.Models;

public record CardModel(string Code, string ImageUrl, int Value, bool FaceDown = false);

public class PlayerHand
{
    public List<CardModel> Cards { get; } = new();
    public decimal Bet { get; set; }
    public bool IsComplete { get; set; }
    public bool IsSplitHand { get; set; }
    public bool DoubledDown { get; set; }
    public string Outcome { get; set; } = string.Empty; // Win, Lose, Push, Blackjack, Bust
    public bool IsBlackjack => Cards.Count == 2 && HandValue() == 21;
    public int HandValue()
    {
        int total = Cards.Where(c => !c.FaceDown).Sum(c => c.Value);
        int aceCount = Cards.Count(c => c.Code.StartsWith("A") && !c.FaceDown);
        while (total > 21 && aceCount > 0)
        {
            total -= 10; // treat one Ace as 1 instead of 11
            aceCount--;
        }
        return total;
    }
}

public class DealerHand
{
    public List<CardModel> Cards { get; } = new();
    public bool RevealHole { get; set; }
    public int HandValue()
    {
        int total = Cards.Where(c => !c.FaceDown || RevealHole).Sum(c => c.Value);
        int aceCount = Cards.Count(c => c.Code.StartsWith("A"));
        while (total > 21 && aceCount > 0)
        {
            total -= 10;
            aceCount--;
        }
        return total;
    }
    public bool IsBlackjack => Cards.Count == 2 && HandValue() == 21 && RevealHole;
}

public class GameState
{
    public string DeckId { get; set; } = string.Empty;
    public int DeckCount { get; set; } = 6;
    public decimal Bankroll { get; set; } = 500m;
    public decimal DefaultBet { get; set; } = 10m;
    public List<PlayerHand> Hands { get; } = new();
    public int ActiveHandIndex { get; set; } = 0;
    public DealerHand Dealer { get; } = new();
    public bool DealerPlaying { get; set; }
    public bool GameResolved { get; set; }
    public decimal RoundNet { get; set; }

    public PlayerHand ActiveHand => Hands[ActiveHandIndex];
}

public class SessionStats
{
    public int HandsPlayed { get; set; }
    public int HandsWon { get; set; }
    public int HandsLost { get; set; }
    public int HandsPushed { get; set; }
    public decimal NetAmount { get; set; }
}
