using System.Collections.Concurrent;
using System.Threading;
using BlackjackRazor.Domain;
using Microsoft.Extensions.Options;

namespace BlackjackRazor.Infrastructure;

public interface IShoeManager
{
    /// <summary>Cards remaining in the current shoe queue.</summary>
    int Remaining { get; }

    /// <summary>
    /// Draws <paramref name="count"/> cards from the shoe. Automatically refreshes the shoe
    /// (creates and loads a brand new shuffled multi-deck) if insufficient cards remain.
    /// </summary>
    Task<IReadOnlyList<Card>> DrawAsync(int count, CancellationToken ct = default);
}

/// <summary>
/// Manages a multi-deck shoe sourced from the external Deck of Cards API. Thread-safe.
/// </summary>
public sealed class ShoeManager : IShoeManager
{
    private readonly IDeckApiClient _client;
    private readonly BlackjackOptions _options;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly Queue<Card> _cards = new();
    private string? _deckId;

    public ShoeManager(IDeckApiClient client, IOptionsMonitor<BlackjackOptions> optMonitor)
    {
        _client = client;
        _options = optMonitor.CurrentValue;
    }

    public int Remaining => _cards.Count;

    public async Task<IReadOnlyList<Card>> DrawAsync(int count, CancellationToken ct = default)
    {
        if (count <= 0) return Array.Empty<Card>();

        await _gate.WaitAsync(ct);
        try
        {
            if (_cards.Count < count)
            {
                await InitializeNewShoeAsync(ct);
            }
            // If still insufficient (API returned fewer cards than requested), attempt another refresh once.
            if (_cards.Count < count)
            {
                await InitializeNewShoeAsync(ct);
            }

            var drawn = new List<Card>(count);
            for (int i = 0; i < count && _cards.Count > 0; i++)
            {
                drawn.Add(_cards.Dequeue());
            }
            return drawn;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task InitializeNewShoeAsync(CancellationToken ct)
    {
        // Create a new shuffled deck consisting of the configured number of decks.
        var deckCount = _options.DefaultDeckCount <= 0 ? 6 : _options.DefaultDeckCount;
        _deckId = await _client.CreateShuffledDeckAsync(deckCount, ct);

        // Attempt to draw all cards from the multi-deck shoe in a single call.
        int target = deckCount * 52;
        var apiCards = await _client.DrawAsync(_deckId, target, ct);

        // Flush any remainder from previous shoe before loading new cards.
        _cards.Clear();

        foreach (var dc in apiCards)
        {
            var mapped = Map(dc);
            if (mapped != null)
            {
                _cards.Enqueue(mapped);
            }
        }
    }

    private static Card? Map(DeckDrawCard dc)
    {
        if (dc.value is null || dc.suit is null) return null;
        CardRank rank = dc.value.ToUpperInvariant() switch
        {
            "ACE" => CardRank.Ace,
            "2" => CardRank.Two,
            "3" => CardRank.Three,
            "4" => CardRank.Four,
            "5" => CardRank.Five,
            "6" => CardRank.Six,
            "7" => CardRank.Seven,
            "8" => CardRank.Eight,
            "9" => CardRank.Nine,
            "10" => CardRank.Ten,
            "JACK" => CardRank.Jack,
            "QUEEN" => CardRank.Queen,
            "KING" => CardRank.King,
            _ => CardRank.Two // Fallback; shouldn't happen with API
        };

        CardSuit suit = dc.suit.ToUpperInvariant() switch
        {
            "CLUBS" => CardSuit.Clubs,
            "DIAMONDS" => CardSuit.Diamonds,
            "HEARTS" => CardSuit.Hearts,
            "SPADES" => CardSuit.Spades,
            _ => CardSuit.Spades
        };

        return new Card(rank, suit);
    }
}
