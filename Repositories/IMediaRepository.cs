namespace MRP_SWEN1.Repositories
{
    // Interface for media persistence (keeps storage implementation swappable).
    public interface IMediaRepository
    {
        Task<int> Create(MediaEntry media);
        Task<MediaEntry?> GetById(int id);
        Task<IEnumerable<MediaEntry>> Search(string titleFilter);
        Task Update(MediaEntry media);
        Task Delete(int id);
    }
}
