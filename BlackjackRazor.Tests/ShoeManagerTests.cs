using System;
using BlackjackRazor.Domain;
using BlackjackRazor.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;

namespace BlackjackRazor.Tests;

public sealed class StubDeckApiClient : IDeckApiClient
{
    private readonly Queue<List<DeckDrawCard>> _decks = new();
    private int _deckIndex = 0;
    private List<DeckDrawCard>? _current;

    public StubDeckApiClient()
    {
        // Two small pseudo-decks (10 cards each) to force refresh logic.
        _decks.Enqueue(GenerateDeck());
        _decks.Enqueue(GenerateDeck());
    }

    private static List<DeckDrawCard> GenerateDeck()
    {
        return new List<DeckDrawCard>
        {
            new() { value = "ACE", suit = "SPADES", code = "AS" },
            new() { value = "10", suit = "HEARTS", code = "0H" },
            new() { value = "KING", suit = "CLUBS", code = "KC" },
            new() { value = "2", suit = "DIAMONDS", code = "2D" },
            new() { value = "3", suit = "DIAMONDS", code = "3D" },
            new() { value = "4", suit = "DIAMONDS", code = "4D" },
            new() { value = "5", suit = "DIAMONDS", code = "5D" },
            new() { value = "QUEEN", suit = "SPADES", code = "QS" },
            new() { value = "JACK", suit = "HEARTS", code = "JH" },
            new() { value = "9", suit = "CLUBS", code = "9C" }
        };
    }

    public Task<string> CreateShuffledDeckAsync(int deckCount, CancellationToken ct = default)
    {
        _current = _decks.Count > 0 ? _decks.Dequeue() : new List<DeckDrawCard>();
        var id = $"deck{_deckIndex++}";
        return Task.FromResult(id);
    }

    public Task<IReadOnlyList<DeckDrawCard>> DrawAsync(string deckId, int count, CancellationToken ct = default)
    {
        if (_current is null || _current.Count == 0)
        {
            return Task.FromResult<IReadOnlyList<DeckDrawCard>>(new List<DeckDrawCard>());
        }
        var take = Math.Min(count, _current.Count);
        var slice = _current.GetRange(0, take);
        _current.RemoveRange(0, take);
        return Task.FromResult<IReadOnlyList<DeckDrawCard>>(slice);
    }
}

public class ShoeManagerTests
{
    [Fact]
    public async Task DrawAsync_InitializesAndMapsCards()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IDeckApiClient, StubDeckApiClient>();
        services.Configure<BlackjackOptions>(o => o.DefaultDeckCount = 1);
        services.AddSingleton<IShoeManager, ShoeManager>();
        var provider = services.BuildServiceProvider();
        var shoe = provider.GetRequiredService<IShoeManager>();

        var first = await shoe.DrawAsync(3);
        Assert.Equal(3, first.Count);
        Assert.Equal(CardRank.Ace, first[0].Rank);
        Assert.Equal(CardRank.Ten, first[1].Rank);
        Assert.Equal(CardRank.King, first[2].Rank);
    }

    [Fact]
    public async Task DrawAsync_RefreshesWhenInsufficient()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IDeckApiClient, StubDeckApiClient>();
        services.Configure<BlackjackOptions>(o => o.DefaultDeckCount = 1);
        services.AddSingleton<IShoeManager, ShoeManager>();
        var provider = services.BuildServiceProvider();
        var shoe = provider.GetRequiredService<IShoeManager>();

        // Consume most of the first pseudo-deck.
        _ = await shoe.DrawAsync(8);
        // Force refresh by requesting more than remaining (first deck had 10 cards).
        var next = await shoe.DrawAsync(5);
        Assert.True(next.Count > 0);
        // First card of second deck should again be an Ace of Spades per stub.
        Assert.Equal(CardRank.Ace, next[0].Rank);
        Assert.Equal(CardSuit.Spades, next[0].Suit);
    }
}
