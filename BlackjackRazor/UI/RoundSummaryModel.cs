using BlackjackRazor.Domain;

namespace BlackjackRazor.UI;

public sealed class RoundSummaryModel
{
    public GameStateSnapshot Snapshot { get; }
    public decimal Net => Snapshot.RoundNetDelta;
    public IReadOnlyList<SettlementHandResult> Results => Snapshot.SettlementResults;
    public RoundSummaryModel(GameStateSnapshot snap) => Snapshot = snap;
}
