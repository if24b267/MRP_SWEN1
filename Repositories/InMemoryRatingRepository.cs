using System.Collections.Concurrent;

namespace MRP_SWEN1.Repositories
{
    // In-memory Ratings repository (thread-safe). Stores ratings keyed by id.
    public class InMemoryRatingRepository : IRatingRepository
    {
        private readonly ConcurrentDictionary<int, Rating> _store = new();
        private int _nextId = 1;

        public Task<int> Create(Rating rating)
        {
            var id = System.Threading.Interlocked.Increment(ref _nextId);
            rating.Id = id;
            _store[id] = rating;
            return Task.FromResult(id);
        }

        public Task<Rating?> GetById(int id)
        {
            _store.TryGetValue(id, out var r);
            return Task.FromResult<Rating?>(r);
        }

        public Task<IEnumerable<Rating>> GetByMediaId(int mediaId)
        {
            var list = _store.Values.Where(r => r.MediaId == mediaId).OrderByDescending(r => r.Timestamp).ToList();
            return Task.FromResult<IEnumerable<Rating>>(list);
        }

        public Task Update(Rating rating)
        {
            if (!_store.ContainsKey(rating.Id))
                throw new KeyNotFoundException("rating not found");
            _store[rating.Id] = rating;
            return Task.CompletedTask;
        }

        public Task Delete(int id)
        {
            _store.TryRemove(id, out _);
            return Task.CompletedTask;
        }
    }
}
