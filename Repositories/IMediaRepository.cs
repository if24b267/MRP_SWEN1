using MRP_SWEN1.Models;

namespace MRP_SWEN1.Repositories
{
    public interface IMediaRepository
    {
        Task<int> Create(MediaEntry media);
        Task<MediaEntry?> GetById(int id);
        Task<IEnumerable<MediaEntry>> Search(string titleFilter);
        Task Update(MediaEntry media);
        Task Delete(int id);
    }
}
