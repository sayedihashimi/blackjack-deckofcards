using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BlackjackRazor.Domain;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace BlackjackRazor.Tests;

public class InMemorySession : ISession
{
    private readonly Dictionary<string, byte[]> _store = new();
    public IEnumerable<string> Keys => _store.Keys;
    public string Id => "test";
    public bool IsAvailable => true;
    public void Clear() => _store.Clear();
    public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public void Remove(string key) => _store.Remove(key);
    public void Set(string key, byte[] value) => _store[key] = value;
    public bool TryGetValue(string key, out byte[] value) => _store.TryGetValue(key, out value!);
}

public class GameStateSerializerTests
{
    private readonly IGameStateSerializer _serializer = new GameStateSerializer();

    [Fact]
    public void RoundTrip_Snapshot_PreservesTotals()
    {
        var hand = new Hand();
        hand.AddCard(new Card(CardRank.Ace, CardSuit.Spades));
        hand.AddCard(new Card(CardRank.King, CardSuit.Hearts));
        var dealer = new Hand();
        dealer.AddCard(new Card(CardRank.Nine, CardSuit.Clubs));
        dealer.AddCard(new Card(CardRank.Seven, CardSuit.Diamonds));
        var snap = new GameStateSnapshot(
            GamePhase.PlayerActing,
            1000m,
            50m,
            0,
            new List<HandSnapshot>{ new(hand.Cards, HandEvaluator.Evaluate(hand), hand.IsCompleted, hand.WasSplitChild, hand.HasDoubled) },
            new HandSnapshot(dealer.Cards, HandEvaluator.Evaluate(dealer), dealer.IsCompleted, dealer.WasSplitChild, dealer.HasDoubled),
            false,
            new List<string>(),
            new List<SettlementHandResult>(),
            0m,
            null);

        var dto = _serializer.ToDto(snap);
        var restored = _serializer.FromDto(dto);
        Assert.Equal(snap.Bankroll, restored.Bankroll);
        Assert.Equal(snap.PlayerHands[0].Evaluation.Total, restored.PlayerHands[0].Evaluation.Total);
        Assert.True(restored.PlayerHands[0].Evaluation.IsBlackjack);
    }

    [Fact]
    public void Session_Save_Load_RoundTrip()
    {
        var session = new InMemorySession();
        var hand = new Hand();
        hand.AddCard(new Card(CardRank.Eight, CardSuit.Spades));
        hand.AddCard(new Card(CardRank.Eight, CardSuit.Clubs));
        var dealer = new Hand();
        dealer.AddCard(new Card(CardRank.Ten, CardSuit.Hearts));
        dealer.AddCard(new Card(CardRank.Six, CardSuit.Diamonds));
        var snap = new GameStateSnapshot(
            GamePhase.PlayerActing,
            500m,
            25m,
            0,
            new List<HandSnapshot>{ new(hand.Cards, HandEvaluator.Evaluate(hand), hand.IsCompleted, hand.WasSplitChild, hand.HasDoubled) },
            new HandSnapshot(dealer.Cards, HandEvaluator.Evaluate(dealer), dealer.IsCompleted, dealer.WasSplitChild, dealer.HasDoubled),
            false,
            new List<string>(),
            new List<SettlementHandResult>(),
            0m,
            null);
        _serializer.Save(session, snap);
        var loaded = _serializer.Load(session);
        Assert.NotNull(loaded);
        Assert.Equal(500m, loaded!.Bankroll);
        Assert.Equal(25m, loaded.CurrentBet);
        Assert.Equal(snap.PlayerHands[0].Evaluation.Total, loaded.PlayerHands[0].Evaluation.Total);
    }
}
