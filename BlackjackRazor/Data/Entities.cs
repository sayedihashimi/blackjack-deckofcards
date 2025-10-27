using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace BlackjackRazor.Data;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<GameSession> GameSessions { get; set; } = new List<GameSession>();
}

public class GameSession
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
    public int DeckCount { get; set; }
    public decimal Bankroll { get; set; }
    public decimal DefaultBet { get; set; }
    public string DeckId { get; set; } = string.Empty;
    public ICollection<HandRecord> Hands { get; set; } = new List<HandRecord>();
    public ICollection<CardDrawn> Cards { get; set; } = new List<CardDrawn>();
}

public class HandRecord
{
    public int Id { get; set; }
    public int GameSessionId { get; set; }
    public GameSession? GameSession { get; set; }
    public int Index { get; set; } // 0,1 when split
    public decimal Bet { get; set; }
    public string Outcome { get; set; } = string.Empty; // Win, Lose, Push, Blackjack, Bust
    public decimal Payout { get; set; }
    public bool IsSplitHand { get; set; }
    public bool IsBlackjack { get; set; }
}

public class CardDrawn
{
    public int Id { get; set; }
    public int GameSessionId { get; set; }
    public GameSession? GameSession { get; set; }
    public int HandIndex { get; set; } // -1 for dealer
    public bool IsDealer { get; set; }
    public string Code { get; set; } = string.Empty; // e.g. "AS"
    public string ImageUrl { get; set; } = string.Empty;
    public int Order { get; set; } // sequence of drawing
    public int Value { get; set; } // numeric value used for total (Aces handled separately)
}

public class StatRecord
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public int HandsPlayed { get; set; }
    public int HandsWon { get; set; }
    public int HandsLost { get; set; }
    public int HandsPushed { get; set; }
    public decimal NetAmount { get; set; }
}

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<GameSession> GameSessions => Set<GameSession>();
    public DbSet<HandRecord> Hands => Set<HandRecord>();
    public DbSet<CardDrawn> Cards => Set<CardDrawn>();
    public DbSet<StatRecord> Stats => Set<StatRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasIndex(u => u.Username).IsUnique();
        modelBuilder.Entity<HandRecord>().HasIndex(h => new { h.GameSessionId, h.Index }).IsUnique();
        base.OnModelCreating(modelBuilder);
    }
}
