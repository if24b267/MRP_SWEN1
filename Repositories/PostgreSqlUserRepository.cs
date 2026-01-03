using System.Threading.Tasks;
using Dapper;
using MRP_SWEN1.Models;
using Npgsql;

namespace MRP_SWEN1.Repositories
{
    public class PostgreSqlUserRepository : IUserRepository
    {
        private readonly string _connStr;
        public PostgreSqlUserRepository(string connStr) => _connStr = connStr;

        public async Task<int> Create(User user)
        {
            using var db = new NpgsqlConnection(_connStr);
            const string sql = @"INSERT INTO users (username, password_hash, salt, favorite_genre)
                                 VALUES (@Username, @PasswordHash, @Salt, @FavoriteGenre)
                                 RETURNING id;";
            return await db.QuerySingleAsync<int>(sql, user);
        }

        public async Task<User?> GetByUsername(string username)
        {
            using var db = new NpgsqlConnection(_connStr);
            const string sql = "SELECT id, username, password_hash, salt, favorite_genre FROM users WHERE LOWER(username) = LOWER(@username);";
            var row = await db.QuerySingleOrDefaultAsync<dynamic>(sql, new { username });
            if (row == null) return null;

            return new User
            {
                Id = row.id,
                Username = row.username,
                PasswordHash = (byte[])row.password_hash,
                Salt = (byte[])row.salt,
                FavoriteGenre = row.favorite_genre
            };
        }

        public async Task<User?> GetById(int id)
        {
            using var db = new NpgsqlConnection(_connStr);
            const string sql = "SELECT * FROM users WHERE id = @id;";
            return await db.QuerySingleOrDefaultAsync<User>(sql, new { id });
        }
    }
}