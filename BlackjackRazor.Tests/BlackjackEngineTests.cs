using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using BlackjackRazor.Models;
using BlackjackRazor.Services;
using Xunit;

namespace BlackjackRazor.Tests;

using CardDto = DeckApiClient.CardDto;

public class BlackjackEngineTests
{

    private class StubDeckApi : DeckApiClient
    {
        private readonly Queue<List<CardDto>> _queues = new();
        public StubDeckApi(params List<CardDto>[] draws) : base(new HttpClient())
        {
            foreach (var d in draws) _queues.Enqueue(d);
        }
        public override Task<string> NewDeckAsync(int deckCount) => Task.FromResult("stub-deck");
        public override Task<List<CardDto>> DrawAsync(string deckId, int count)
        {
            var next = _queues.Dequeue();
            return Task.FromResult(next);
        }
    }

    private static CardDto C(string code, int value) => new() { Code = code, Image = "img", Value = value >= 2 && value <= 10 ? value.ToString() : value == 11 ? "ACE" : value.ToString() };

    [Fact]
    public async Task SplitCreatesTwoHandsAndDealsExtraCards()
    {
        // Initial draw: P1, D1, P2, D2(hole) -> ensure player has pair (8,8)
        var initial = new List<CardDto>{ C("8H",8), C("5S",5), C("8D",8), C("KC",10)};
        // Split draw: one card to each hand
        var splitDraw = new List<CardDto>{ C("2S",2), C("3D",3)};
        var api = new StubDeckApi(initial, splitDraw);
        var engine = new BlackjackEngine(api);
        var state = new GameState();
        await engine.InitializeAsync(state);
        Assert.Equal(1, state.Hands.Count);
        Assert.True(engine.CanSplit(state));
        await engine.SplitAsync(state);
        Assert.Equal(2, state.Hands.Count);
        Assert.Equal(2, state.Hands[0].Cards.Count); // 8 + new card
        Assert.Equal(2, state.Hands[1].Cards.Count); // 8 + new card
    }

    [Fact]
    public async Task DoubleDownDoublesBetAndCompletesHand()
    {
        var initial = new List<CardDto>{ C("5H",5), C("9S",9), C("6D",6), C("QH",10)}; // player 5+6=11 ideal double
        var hitDraw = new List<CardDto>{ C("5D",5)};
        var api = new StubDeckApi(initial, hitDraw);
        var engine = new BlackjackEngine(api);
        var state = new GameState();
        await engine.InitializeAsync(state);
        var originalBet = state.ActiveHand.Bet;
        Assert.True(engine.CanDouble(state));
        await engine.DoubleDownAsync(state);
        Assert.Equal(originalBet * 2, state.Hands[0].Bet);
        Assert.True(state.Hands[0].IsComplete);
    }

    [Fact]
    public async Task PushDoesNotChangeBankroll()
    {
        var initial = new List<CardDto>{ C("9H",9), C("5S",5), C("8D",8), C("7C",7)}; // player 9+8=17 dealer 5 + 7(hidden) = 12
        var dealerHits = new List<CardDto>{ C("5D",5)}; // dealer 17
        var api = new StubDeckApi(initial, dealerHits);
        var engine = new BlackjackEngine(api);
        var state = new GameState();
        var startBankroll = state.Bankroll;
        await engine.InitializeAsync(state);
        // Stand immediately both draws complete dealer play
        await engine.StandAsync(state);
        Assert.True(state.GameResolved);
        Assert.Equal(startBankroll, state.Bankroll); // push roundNet zero
        Assert.Equal(0m, state.RoundNet);
        Assert.Equal("Push", state.Hands[0].Outcome);
    }

    [Fact]
    public async Task BustSubtractsBet()
    {
        var initial = new List<CardDto>{ C("9H",9), C("5S",5), C("8D",8), C("7C",7)}; // player 9+8=17
        var hit = new List<CardDto>{ C("9D",9)}; // bust 26 -> adjust no Aces
        var dealerHits = new List<CardDto>{ C("5D",5)}; // dealer irrelevant
        var api = new StubDeckApi(initial, hit, dealerHits);
        var engine = new BlackjackEngine(api);
        var state = new GameState();
        var startBankroll = state.Bankroll;
        await engine.InitializeAsync(state);
        await engine.HitAsync(state); // now bust, completes
        while(!state.GameResolved) await engine.StandAsync(state);
        Assert.True(state.GameResolved);
        Assert.Equal(startBankroll - state.Hands[0].Bet, state.Bankroll);
        Assert.Equal("Bust", state.Hands[0].Outcome);
    }
}
