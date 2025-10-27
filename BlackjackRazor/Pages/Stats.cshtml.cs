using Microsoft.AspNetCore.Mvc.RazorPages;
using BlackjackRazor.Data;
using BlackjackRazor.Models;
using System.Linq;
using System.Text.Json;

namespace BlackjackRazor.Pages;

public class StatsModel : PageModel
{
    private readonly AppDbContext _db;
    public StatsModel(AppDbContext db){ _db = db; }

    public string? Username { get; set; }
    public SessionStats Session { get; set; } = new();
    public int TotalHands { get; set; }
    public int TotalWon { get; set; }
    public int TotalLost { get; set; }
    public int TotalPushed { get; set; }
    public decimal TotalNet { get; set; }
    public List<StatRecord> Recent { get; set; } = new();

    public void OnGet()
    {
        Username = User.Identity?.Name;
        var statsData = HttpContext.Session.Get("stats");
        if (statsData != null) Session = System.Text.Json.JsonSerializer.Deserialize<SessionStats>(statsData) ?? new SessionStats();
        if (Username == null) return;
        var user = _db.Users.FirstOrDefault(u => u.Username == Username);
        if (user == null) return;
        var all = _db.Stats.Where(s => s.UserId == user.Id).OrderByDescending(s => s.Timestamp).ToList();
        TotalHands = all.Sum(s => s.HandsPlayed);
        TotalWon = all.Sum(s => s.HandsWon);
        TotalLost = all.Sum(s => s.HandsLost);
        TotalPushed = all.Sum(s => s.HandsPushed);
        TotalNet = all.Sum(s => s.NetAmount);
        Recent = all.Take(20).ToList();
    }
}
