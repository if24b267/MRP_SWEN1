using System.Collections.Concurrent;

namespace MRP_SWEN1.Repositories
{
    // Simple thread-safe in-memory repository.
    // I used ConcurrentDictionary so multiple requests can be handled concurrently.
    public class InMemoryMediaRepository : IMediaRepository
    {
        private readonly ConcurrentDictionary<int, MediaEntry> _store = new();
        private int _nextId = 1;

        public Task<int> Create(MediaEntry media)
        {
            var id = System.Threading.Interlocked.Increment(ref _nextId);
            media.Id = id;
            media.Genres = media.Genres?.ToList() ?? new List<string>();
            _store[id] = media;

            return Task.FromResult(id);
        }

        public Task<MediaEntry?> GetById(int id)
        {
            _store.TryGetValue(id, out var m);
            return Task.FromResult<MediaEntry?>(m);
        }

        public Task<IEnumerable<MediaEntry>> Search(string titleFilter)
        {
            var query = titleFilter?.Trim().ToLowerInvariant() ?? "";

            var res = _store.Values
                .Where(media => string.IsNullOrEmpty(query) || media.Title.ToLowerInvariant().Contains(query))
                .OrderBy(media => media.Title)
                .ToList()
                .AsEnumerable();

            return Task.FromResult<IEnumerable<MediaEntry>>(res);
        }

        public Task Update(MediaEntry media)
        {
            if (!_store.ContainsKey(media.Id))
            {
                throw new KeyNotFoundException("media not found");
            }

            media.Genres = media.Genres?.ToList() ?? new List<string>();
            _store[media.Id] = media;

            return Task.CompletedTask;
        }

        public Task Delete(int id)
        {
            _store.TryRemove(id, out _);
            return Task.CompletedTask;
        }
    }
}
