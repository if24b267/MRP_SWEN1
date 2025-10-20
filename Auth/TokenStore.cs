using System.Collections.Concurrent;

namespace MRP_SWEN1.Auth
{
    // Simple DTO that holds token metadata.
    // I wrote this to keep token info together (token string, user id, username, created timestamp).
    public class TokenInfo
    {
        public string Token { get; set; } = "";
        public int UserId { get; set; }
        public string Username { get; set; } = "";
        public DateTime Created { get; set; } = DateTime.UtcNow;
    }

    // In-memory token store used for authentication in the intermediate hand-in.
    // This uses a thread-safe ConcurrentDictionary and is intentionally simple:
    // tokens are kept in memory instead of a DB.
    public class TokenStore
    {
        // Thread-safe dictionary, so multiple requests can read/write safely.
        private readonly ConcurrentDictionary<string, TokenInfo> _store = new();

        // Add or replace token
        public void Add(string token, TokenInfo info) => _store[token] = info;

        // Try to lookup a token
        public bool TryGet(string token, out TokenInfo info) => _store.TryGetValue(token, out info);

        // Remove token (not used everywhere, but handy for logout if implemented)
        public void Remove(string token) => _store.TryRemove(token, out _);
    }
}
