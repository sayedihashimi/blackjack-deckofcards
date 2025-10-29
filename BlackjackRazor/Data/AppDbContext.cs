using Microsoft.EntityFrameworkCore;

namespace BlackjackRazor.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}

    public DbSet<User> Users => Set<User>();
    public DbSet<Game> Games => Set<Game>();
    public DbSet<Hand> Hands => Set<Hand>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<User>().HasIndex(u => u.Username).IsUnique();
        modelBuilder.Entity<Game>().HasIndex(g => g.UserId);
        modelBuilder.Entity<Hand>().HasIndex(h => new { h.GameId, h.HandIndex }).IsUnique();
    }
}
