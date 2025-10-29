using System.Text.Json;
using Microsoft.AspNetCore.Http;
using BlackjackRazor.Data; // for HandOutcome enum

namespace BlackjackRazor.Domain;

public sealed record GameStateDto(
    GamePhase Phase,
    decimal Bankroll,
    decimal CurrentBet,
    int ActiveHandIndex,
    List<GameStateHandDto> PlayerHands,
    GameStateHandDto Dealer,
    bool DealerPlayed,
    List<string> Events,
    List<GameStateSettlementResultDto>? SettlementResults,
    decimal RoundNetDelta,
    int? GameId
);

public sealed record GameStateSettlementResultDto(int HandIndex, HandOutcome Outcome, decimal Bet, decimal Payout, decimal NetDelta);

public sealed record GameStateHandDto(List<string> Cards, bool IsCompleted, bool WasSplitChild, bool HasDoubled);

public interface IGameStateSerializer
{
    string SessionKey { get; }
    GameStateDto ToDto(GameStateSnapshot snapshot);
    GameStateSnapshot FromDto(GameStateDto dto);
    void Save(ISession session, GameStateSnapshot snapshot);
    GameStateSnapshot? Load(ISession session);
}

public sealed class GameStateSerializer : IGameStateSerializer
{
    public string SessionKey => "bj_game_state";
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    public GameStateDto ToDto(GameStateSnapshot snapshot)
    {
        return new GameStateDto(
            snapshot.Phase,
            snapshot.Bankroll,
            snapshot.CurrentBet,
            snapshot.ActiveHandIndex,
            snapshot.PlayerHands.Select(MapHand).ToList(),
            MapHand(snapshot.Dealer),
            snapshot.DealerPlayed,
            snapshot.Events.ToList(),
            snapshot.SettlementResults.Select(r => new GameStateSettlementResultDto(r.HandIndex, r.Outcome, r.Bet, r.Payout, r.NetDelta)).ToList(),
            snapshot.RoundNetDelta,
            snapshot.GameId
        );
    }

    public GameStateSnapshot FromDto(GameStateDto dto)
    {
        var playerHands = dto.PlayerHands.Select(h => RehydrateHand(h)).ToList();
        var dealerHand = RehydrateHand(dto.Dealer);
        var events = dto.Events ?? new List<string>();
        var settlement = (dto.SettlementResults ?? new List<GameStateSettlementResultDto>())
            .Select(s => new SettlementHandResult(s.HandIndex, s.Outcome, s.Bet, s.Payout, s.NetDelta)).ToList();
        return new GameStateSnapshot(
            dto.Phase,
            dto.Bankroll,
            dto.CurrentBet,
            dto.ActiveHandIndex,
            playerHands.Select(h => new HandSnapshot(h.Cards, HandEvaluator.Evaluate(h), h.IsCompleted, h.WasSplitChild, h.HasDoubled)).ToList(),
            new HandSnapshot(dealerHand.Cards, HandEvaluator.Evaluate(dealerHand), dealerHand.IsCompleted, dealerHand.WasSplitChild, dealerHand.HasDoubled),
            dto.DealerPlayed,
            events,
            settlement,
            dto.RoundNetDelta,
            dto.GameId
        );
    }

    public void Save(ISession session, GameStateSnapshot snapshot)
    {
        var dto = ToDto(snapshot);
        var json = JsonSerializer.Serialize(dto, _json);
        session.SetString(SessionKey, json);
    }

    public GameStateSnapshot? Load(ISession session)
    {
        var json = session.GetString(SessionKey);
        if (string.IsNullOrEmpty(json)) return null;
        var dto = JsonSerializer.Deserialize<GameStateDto>(json, _json);
        if (dto is null) return null;
        return FromDto(dto);
    }

    private static GameStateHandDto MapHand(HandSnapshot snap)
    {
        var codes = snap.Cards.Select(c => c.ToShortCode()).ToList();
        return new GameStateHandDto(codes, snap.IsCompleted, snap.WasSplitChild, snap.HasDoubled);
    }

    private static Hand RehydrateHand(GameStateHandDto dto)
    {
        var hand = new Hand { WasSplitChild = dto.WasSplitChild };
        foreach (var code in dto.Cards)
        {
            var card = Parse(code);
            if (card != null) hand.AddCard(card);
        }
        if (dto.IsCompleted) hand.MarkCompleted();
        if (dto.HasDoubled) hand.MarkDoubled();
        return hand;
    }

    private static Card? Parse(string code)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length < 2) return null;
        // Rank part: could be A,K,Q,J,0 for Ten or numeric 2-9
        char r = code[0];
        char s = code[^1];
        CardRank rank = r switch
        {
            'A' => CardRank.Ace,
            'K' => CardRank.King,
            'Q' => CardRank.Queen,
            'J' => CardRank.Jack,
            '0' => CardRank.Ten,
            '2' => CardRank.Two,
            '3' => CardRank.Three,
            '4' => CardRank.Four,
            '5' => CardRank.Five,
            '6' => CardRank.Six,
            '7' => CardRank.Seven,
            '8' => CardRank.Eight,
            '9' => CardRank.Nine,
            _ => CardRank.Two
        };
        CardSuit suit = s switch
        {
            'C' => CardSuit.Clubs,
            'D' => CardSuit.Diamonds,
            'H' => CardSuit.Hearts,
            'S' => CardSuit.Spades,
            _ => CardSuit.Spades
        };
        return new Card(rank, suit);
    }
}
