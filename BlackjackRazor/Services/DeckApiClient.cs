using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace BlackjackRazor.Services;

public class DeckApiClient
{
    private readonly HttpClient _http;

    public DeckApiClient(HttpClient http)
    {
        _http = http;
    }

    public virtual async Task<string> NewShuffledDeckAsync(int deckCount, CancellationToken ct = default)
    {
        var resp = await _http.GetFromJsonAsync<NewDeckResponse>($"api/deck/new/shuffle/?deck_count={deckCount}", cancellationToken: ct);
        if (resp == null || !resp.Success || string.IsNullOrWhiteSpace(resp.DeckId))
            throw new InvalidOperationException("Failed to create deck");
        return resp.DeckId;
    }

    public virtual async Task<List<Card>> DrawAsync(string deckId, int count, CancellationToken ct = default)
    {
        var resp = await _http.GetFromJsonAsync<DrawResponse>($"api/deck/{deckId}/draw/?count={count}", cancellationToken: ct);
        if (resp == null || !resp.Success)
            throw new InvalidOperationException("Failed to draw cards");
        return resp.Cards;
    }

    public virtual async Task ReshuffleAsync(string deckId, CancellationToken ct = default)
    {
        var resp = await _http.GetFromJsonAsync<GenericResponse>($"api/deck/{deckId}/shuffle/", cancellationToken: ct);
        if (resp == null || !resp.Success)
            throw new InvalidOperationException("Failed to reshuffle deck");
    }

    private record GenericResponse(
        [property: JsonPropertyName("success")] bool Success,
        [property: JsonPropertyName("deck_id")] string DeckId,
        [property: JsonPropertyName("remaining")] int Remaining,
        [property: JsonPropertyName("shuffled")] bool Shuffled
    );

    private record NewDeckResponse(
        [property: JsonPropertyName("success")] bool Success,
        [property: JsonPropertyName("deck_id")] string DeckId,
        [property: JsonPropertyName("remaining")] int Remaining,
        [property: JsonPropertyName("shuffled")] bool Shuffled
    );

    private record DrawResponse(
        [property: JsonPropertyName("success")] bool Success,
        [property: JsonPropertyName("deck_id")] string DeckId,
        [property: JsonPropertyName("remaining")] int Remaining,
        [property: JsonPropertyName("cards")] List<Card> Cards
    );

    public record Card(
        [property: JsonPropertyName("code")] string Code,
        [property: JsonPropertyName("image")] string Image,
        [property: JsonPropertyName("value")] string Value,
        [property: JsonPropertyName("suit")] string Suit
    );
}
