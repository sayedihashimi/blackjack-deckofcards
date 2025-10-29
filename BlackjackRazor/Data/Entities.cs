using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlackjackRazor.Data;

public class User
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(32)]
    public string Username { get; set; } = string.Empty;

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}

public class Game
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(User))]
    public int UserId { get; set; }
    public User? User { get; set; }

    public int DeckCount { get; set; }
    [MaxLength(64)] public string DeckId { get; set; } = string.Empty; // External deck API id

    public DateTime StartedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? EndedUtc { get; set; }

    public int DefaultBet { get; set; }
    public decimal BankrollStart { get; set; }
    public decimal BankrollEnd { get; set; }

    // Aggregate stats snapshot for persistence
    public int HandsPlayed { get; set; }
    public int PlayerBlackjacks { get; set; }
    public int DealerBlackjacks { get; set; }
    public int PlayerBusts { get; set; }
    public int DealerBusts { get; set; }
    public int PlayerWins { get; set; }
    public int DealerWins { get; set; }
    public int Pushes { get; set; }
}

public enum HandOutcome
{
    Pending = 0,
    Blackjack,
    Win,
    Lose,
    Push,
    Bust
}

public class Hand
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(Game))]
    public int GameId { get; set; }
    public Game? Game { get; set; }

    public int HandIndex { get; set; }
    public decimal Bet { get; set; }
    public HandOutcome Outcome { get; set; } = HandOutcome.Pending;
    public decimal Payout { get; set; }
    public bool WasSplit { get; set; }
    public bool WasDouble { get; set; }
    public DateTime PlayedUtc { get; set; } = DateTime.UtcNow;
}
