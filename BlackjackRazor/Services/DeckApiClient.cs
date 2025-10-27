using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BlackjackRazor.Services;

public class DeckApiClient
{
    private readonly HttpClient _http;
    public DeckApiClient(HttpClient http) => _http = http;

    public virtual async Task<string> NewDeckAsync(int deckCount)
    {
        var resp = await _http.GetFromJsonAsync<NewDeckResponse>($"/api/deck/new/shuffle/?deck_count={deckCount}");
        return resp?.DeckId ?? string.Empty;
    }

    public virtual async Task<List<CardDto>> DrawAsync(string deckId, int count)
    {
        var resp = await _http.GetFromJsonAsync<DrawResponse>($"/api/deck/{deckId}/draw/?count={count}");
        return resp?.Cards ?? new List<CardDto>();
    }

    public class NewDeckResponse { [JsonPropertyName("deck_id")] public string DeckId { get; set; } = string.Empty; }
    public class DrawResponse { [JsonPropertyName("cards")] public List<CardDto> Cards { get; set; } = new(); }
    public class CardDto
    {
        [JsonPropertyName("code")] public string Code { get; set; } = string.Empty;
        [JsonPropertyName("image")] public string Image { get; set; } = string.Empty;
        [JsonPropertyName("value")] public string Value { get; set; } = string.Empty; // "ACE", "KING", etc.
        [JsonPropertyName("suit")] public string Suit { get; set; } = string.Empty;

        public int NumericValue => Value switch
        {
            "ACE" => 11,
            "KING" or "QUEEN" or "JACK" => 10,
            _ => int.TryParse(Value, out var v) ? v : 0
        };
    }
}
