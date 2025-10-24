using BlackjackRazor.Services;
using BlackjackRazor.Engine;
using Xunit;
using System.Collections.Generic;
using System.Threading.Tasks;

public class BlackjackEngineTests
{
    private static DeckApiClient.Card Card(string value, string suit="HEARTS") => new(value, $"img/{value}{suit}.png", value, suit);

    [Theory]
    [InlineData(new[]{"ACE","KING"},21,true)]
    [InlineData(new[]{"ACE","9"},20,false)]
    [InlineData(new[]{"ACE","9","ACE"},21,false)]
    [InlineData(new[]{"ACE","9","ACE","ACE"},12,false)]
    public void Evaluate_AceHandling(string[] cardValues, int expectedTotal, bool expectedBJ)
    {
        var cards = new List<DeckApiClient.Card>();
        foreach (var v in cardValues) cards.Add(Card(v));
        var hv = BlackjackEngine.Evaluate(cards);
        Assert.Equal(expectedTotal, hv.Total);
        Assert.Equal(expectedBJ, hv.IsBlackjack);
    }

    [Fact]
    public async Task Dealer_Hits_Soft17()
    {
        var state = new GameState();
        state.DealerCards.AddRange(new[]{ Card("ACE"), Card("6")}); // soft 17 triggers hit
        var fake = new FakeDeckApi(new(){ Card("2"), Card("5")}); // ensure it draws at least one
        await BlackjackEngine.DealerPlayAsync(state, fake);
        Assert.True(state.DealerCards.Count >=3); // drew at least one card
    }

    [Fact]
    public void CanSplit_WhenSameRank()
    {
        var hand = new PlayerHand{ Cards = new(){ Card("8"), Card("8")}};
        Assert.True(BlackjackEngine.CanSplit(hand));
    }

    [Fact]
    public void CanDouble_FirstTwoCards()
    {
        var hand = new PlayerHand{ Cards = new(){ Card("5"), Card("6")}};
        Assert.True(BlackjackEngine.CanDouble(hand));
    }

    [Fact]
    public void Payout_Blackjack_ThreeToTwo()
    {
        var hand = new PlayerHand{ Bet=100, Cards = new(){ Card("ACE"), Card("KING")}, Result=HandResult.Blackjack};
        var hv = BlackjackEngine.Evaluate(hand.Cards);
        var payout = BlackjackEngine.Payout(hand, hv, hv);
        Assert.Equal(250, payout); // 2.5 * bet (original + 1.5 win)
    }

    private class FakeDeckApi : DeckApiClient
    {
        private Queue<DeckApiClient.Card> _queue;
        public FakeDeckApi(List<DeckApiClient.Card> cards) : base(new HttpClient(new FakeHandler()))
        { _queue = new Queue<DeckApiClient.Card>(cards); }
        public override Task<List<DeckApiClient.Card>> DrawAsync(string deckId, int count, System.Threading.CancellationToken ct = default)
        { var list = new List<DeckApiClient.Card>(); for(int i=0;i<count && _queue.Count>0;i++) list.Add(_queue.Dequeue()); return Task.FromResult(list); }
        private class FakeHandler : HttpMessageHandler { protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)=> Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK){ Content = new StringContent("{}")}); }
    }
}
