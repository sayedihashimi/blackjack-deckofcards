namespace BlackjackRazor.Infrastructure;

/// <summary>
/// Configuration options bound from the "Blackjack" section of appsettings.json.
/// </summary>
public sealed class BlackjackOptions
{
    /// <summary>
    /// Number of decks to include in a shoe. Defaults to 6 if not configured.
    /// </summary>
    public int DefaultDeckCount { get; set; } = 6;
    /// <summary>
    /// Whether dealer should hit on soft 17. Defaults to false.
    /// </summary>
    public bool DealerHitSoft17 { get; set; } = false;
    /// <summary>
    /// Delay in milliseconds between dealer drawing cards during automatic play.
    /// Defaults to 2000 (2 seconds).
    /// </summary>
    public int DealerCardDelayMs { get; set; } = 2000;
}
