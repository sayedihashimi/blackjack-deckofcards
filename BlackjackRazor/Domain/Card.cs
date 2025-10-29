namespace BlackjackRazor.Domain;

public enum CardRank
{
    Two = 2,
    Three,
    Four,
    Five,
    Six,
    Seven,
    Eight,
    Nine,
    Ten,
    Jack,
    Queen,
    King,
    Ace
}

public enum CardSuit
{
    Clubs,
    Diamonds,
    Hearts,
    Spades
}

public sealed class Card
{
    public CardRank Rank { get; }
    public CardSuit Suit { get; }

    public Card(CardRank rank, CardSuit suit)
    {
        Rank = rank;
        Suit = suit;
    }

    public bool IsAce => Rank == CardRank.Ace;
    public int BaseValue => Rank switch
    {
        CardRank.Ace => 1, // treat as 1 baseline; evaluator decides 11
        CardRank.Jack or CardRank.Queen or CardRank.King => 10,
        _ => (int)Rank
    };

    public override string ToString() => $"{Rank} of {Suit}";

    public string ToShortCode()
    {
        // For external API mapping (e.g. deckofcardsapi) if needed later.
        var rankCode = Rank switch
        {
            CardRank.Ten => "0", // sometimes T or 0 depending on API
            CardRank.Jack => "J",
            CardRank.Queen => "Q",
            CardRank.King => "K",
            CardRank.Ace => "A",
            _ => ((int)Rank).ToString()
        };
        var suitCode = Suit switch
        {
            CardSuit.Clubs => "C",
            CardSuit.Diamonds => "D",
            CardSuit.Hearts => "H",
            CardSuit.Spades => "S",
            _ => "?"
        };
        return rankCode + suitCode;
    }
}
