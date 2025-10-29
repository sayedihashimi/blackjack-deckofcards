namespace BlackjackRazor.Services;

public record UserRecord(string Username, DateTime CreatedUtc);

public interface IUserStore
{
    Task<UserRecord> UpsertAsync(string username);
    Task<UserRecord?> GetAsync(string username);
}

public class InMemoryUserStore : IUserStore
{
    private readonly Dictionary<string, UserRecord> _users = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();

    public Task<UserRecord> UpsertAsync(string username)
    {
        lock (_lock)
        {
            if (_users.TryGetValue(username, out var existing))
                return Task.FromResult(existing);
            var created = new UserRecord(username, DateTime.UtcNow);
            _users[username] = created;
            return Task.FromResult(created);
        }
    }

    public Task<UserRecord?> GetAsync(string username)
    {
        lock (_lock)
        {
            _users.TryGetValue(username, out var user);
            return Task.FromResult(user);
        }
    }
}
