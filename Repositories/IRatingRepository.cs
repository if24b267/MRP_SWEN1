namespace MRP_SWEN1.Repositories
{
    public interface IRatingRepository
    {
        Task<int> Create(Rating rating);
        Task<Rating?> GetById(int id);
        Task<IEnumerable<Rating>> GetByMediaId(int mediaId);
        Task Update(Rating rating);
        Task Delete(int id);
    }
}
