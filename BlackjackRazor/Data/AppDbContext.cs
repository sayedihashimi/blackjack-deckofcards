using Microsoft.EntityFrameworkCore;

namespace BlackjackRazor.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<GameHistory> GameHistories { get; set; }
        public DbSet<Stat> Stats { get; set; }
    }

    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
    }

    public class GameHistory
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
        public DateTime PlayedAt { get; set; }
        public int Bet { get; set; }
        public int Payout { get; set; }
        public string Result { get; set; }
    }

    public class Stat
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
        public int GamesPlayed { get; set; }
        public int GamesWon { get; set; }
        public int GamesLost { get; set; }
        public int Blackjacks { get; set; }
        public int TotalBet { get; set; }
        public int TotalPayout { get; set; }
    }
}
