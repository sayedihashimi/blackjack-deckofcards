using BlackjackRazor.Services;
using BlackjackRazor.Engine;

namespace BlackjackRazor.Models;

public class SessionStats
{
    public int HandsPlayed { get; set; }
    public int HandsWon { get; set; }
    public int HandsLost { get; set; }
    public int HandsPushed { get; set; }
    public int Blackjacks { get; set; }
    public int Bankroll { get; set; } = 1000; // starting bankroll default
    public int LastNet { get; set; }
    public string? LastSummary { get; set; }
}

public class UserSessionState
{
    public GameState Game { get; set; } = new();
    public SessionStats Stats { get; set; } = new();
}

public interface IGameSessionStore
{
    UserSessionState GetOrCreate(string username);
}

public class InMemoryGameSessionStore : IGameSessionStore
{
    private readonly Dictionary<string, UserSessionState> _sessions = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();

    public UserSessionState GetOrCreate(string username)
    {
        lock (_lock)
        {
            if (!_sessions.TryGetValue(username, out var state))
            {
                state = new UserSessionState();
                _sessions[username] = state;
            }
            return state;
        }
    }
}
