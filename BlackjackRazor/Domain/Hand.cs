using System.Collections.ObjectModel;

namespace BlackjackRazor.Domain;

public sealed class Hand
{
    private readonly List<Card> _cards = new();
    public IReadOnlyList<Card> Cards => new ReadOnlyCollection<Card>(_cards);

    public bool IsCompleted { get; private set; }
    public bool WasSplitChild { get; init; }
    public bool HasDoubled { get; private set; }

    public void AddCard(Card card)
    {
        if (IsCompleted) throw new InvalidOperationException("Cannot add card to completed hand.");
        _cards.Add(card);
    }

    public void MarkCompleted() => IsCompleted = true;
    public void MarkDoubled() => HasDoubled = true;
}
