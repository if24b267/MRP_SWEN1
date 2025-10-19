using System.Collections.Concurrent;

namespace MRP_SWEN1.Repositories
{
    // Simple user repository used by AuthService. Username map is case-insensitive.
    public class InMemoryUserRepository : IUserRepository
    {
        private readonly ConcurrentDictionary<string, User> _byUsername = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<int, User> _byId = new();
        private int _nextId = 1;

        public Task<int> Create(User user)
        {
            var id = System.Threading.Interlocked.Increment(ref _nextId);
            user.Id = id;
            _byUsername[user.Username] = user;
            _byId[id] = user;

            return Task.FromResult(id);
        }

        public Task<User?> GetByUsername(string username)
        {
            if (_byUsername.TryGetValue(username, out var u))
            {
                return Task.FromResult<User?>(u);
            }

            return Task.FromResult<User?>(null);
        }

        public Task<User?> GetById(int id)
        {
            if (_byId.TryGetValue(id, out var u)) 
            { 
                return Task.FromResult<User?>(u); 
            }

            return Task.FromResult<User?>(null);
        }
    }
}
