using BlackjackRazor.Models;
using BlackjackRazor.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

public class StatsModel : PageModel
{
    private readonly IGameSessionStore _store;
    private readonly AppDbContext _db;
    public SessionStats? SessionStats { get; set; }
    public Stat? DbStat { get; set; }

    public StatsModel(IGameSessionStore store, AppDbContext db)
    { _store = store; _db = db; }

    public async Task OnGetAsync()
    {
        if (User?.Identity?.IsAuthenticated ?? false)
        {
            var state = _store.GetOrCreate(User.Identity!.Name!);
            SessionStats = state.Stats;
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == User.Identity.Name);
            if (user != null)
            {
                DbStat = await _db.Stats.FirstOrDefaultAsync(s => s.UserId == user.Id);
            }
        }
    }
}