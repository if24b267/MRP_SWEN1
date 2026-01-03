    using MRP_SWEN1.Models;

    namespace MRP_SWEN1.Repositories
    {
        public interface IUserRepository
        {
            Task<User?> GetByUsername(string username);
            Task<User?> GetById(int id);
            Task<int> Create(User user);
        }
    }
