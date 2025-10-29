using System.Net.Http.Json;
using System.Text.Json;

namespace BlackjackRazor.Infrastructure;

public interface IDeckApiClient
{
    Task<string> CreateShuffledDeckAsync(int deckCount, CancellationToken ct = default);
    Task<IReadOnlyList<DeckDrawCard>> DrawAsync(string deckId, int count, CancellationToken ct = default);
}

public sealed class DeckApiClient : IDeckApiClient
{
    private readonly HttpClient _http;
    private readonly JsonSerializerOptions _jsonOpts = new(JsonSerializerDefaults.Web);

    public DeckApiClient(HttpClient http)
    {
        _http = http;
        // BaseAddress set via Program.cs registration
    }

    public async Task<string> CreateShuffledDeckAsync(int deckCount, CancellationToken ct = default)
    {
        var url = $"/api/deck/new/shuffle/?deck_count={deckCount}";
        var resp = await SendWithRetryAsync(() => _http.GetAsync(url, ct));
        resp.EnsureSuccessStatusCode();
        var payload = await resp.Content.ReadFromJsonAsync<DeckCreateResponse>(_jsonOpts, ct)
                      ?? throw new InvalidOperationException("Null deck create response");
        return payload.deck_id ?? throw new InvalidOperationException("Missing deck id");
    }

    public async Task<IReadOnlyList<DeckDrawCard>> DrawAsync(string deckId, int count, CancellationToken ct = default)
    {
        var url = $"/api/deck/{deckId}/draw/?count={count}";
        var resp = await SendWithRetryAsync(() => _http.GetAsync(url, ct));
        resp.EnsureSuccessStatusCode();
        var payload = await resp.Content.ReadFromJsonAsync<DeckDrawResponse>(_jsonOpts, ct)
                  ?? throw new InvalidOperationException("Null draw response");
        return payload.cards is null ? Array.Empty<DeckDrawCard>() : payload.cards.AsReadOnly();
    }

    private async Task<HttpResponseMessage> SendWithRetryAsync(Func<Task<HttpResponseMessage>> send)
    {
        const int maxAttempts = 3; // initial + 2 retries
        int attempt = 0;
        HttpResponseMessage? last = null;
        while (attempt < maxAttempts)
        {
            try
            {
                last = await send();
                if ((int)last.StatusCode >= 500)
                {
                    attempt++;
                    if (attempt < maxAttempts)
                    {
                        await Task.Delay(200 * attempt); // backoff
                        continue;
                    }
                }
                return last;
            }
            catch (HttpRequestException) when (attempt < maxAttempts - 1)
            {
                attempt++;
                await Task.Delay(200 * attempt);
            }
        }
        return last ?? throw new HttpRequestException("Deck API request failed after retries.");
    }
}

public sealed class DeckCreateResponse
{
    public bool success { get; set; }
    public string? deck_id { get; set; }
    public int remaining { get; set; }
    public bool shuffled { get; set; }
}

public sealed class DeckDrawResponse
{
    public bool success { get; set; }
    public string? deck_id { get; set; }
    public int remaining { get; set; }
    public List<DeckDrawCard>? cards { get; set; }
}

public sealed class DeckDrawCard
{
    public string? code { get; set; } // e.g., AS, 0H, KD
    public string? image { get; set; }
    public string? value { get; set; } // "ACE", "10" etc.
    public string? suit { get; set; }  // "SPADES" etc.
}
