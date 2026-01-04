using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using MRP_SWEN1.Models;
using Npgsql;

namespace MRP_SWEN1.Repositories
{
    public class PostgreSqlRatingRepository : IRatingRepository
    {
        private readonly string _connStr;
        public PostgreSqlRatingRepository(string connStr) => _connStr = connStr;

        public async Task<int> Create(Rating rating)
        {
            using var db = new NpgsqlConnection(_connStr);
            const string sql = @"INSERT INTO ratings (media_id, user_id, stars, comment, confirmed)
                                 VALUES (@MediaId, @UserId, @Stars, @Comment, @Confirmed)
                                 RETURNING id;";
            var id = await db.QuerySingleAsync<int>(sql, rating);
            rating.Id = id;
            return id;
        }

        public async Task<Rating?> GetById(int id)
        {
            using var db = new NpgsqlConnection(_connStr);
            const string sql = "SELECT * FROM ratings WHERE id = @id;";
            return await db.QuerySingleOrDefaultAsync<Rating>(sql, new { id });
        }

        public async Task<IEnumerable<Rating>> GetByMediaId(int mediaId)
        {
            using var db = new NpgsqlConnection(_connStr);
            const string sql = @"SELECT r.id, r.media_id AS MediaId, r.user_id AS UserId, r.stars, r.comment, 
                                        r.confirmed, r.timestamp AS CreatedAt,
                                        u.username
                                 FROM   ratings r
                                 JOIN   users   u ON u.id = r.user_id
                                 WHERE  r.media_id = @mediaId
                                 AND    r.confirmed = true
                                 ORDER  BY r.timestamp DESC;";
            return await db.QueryAsync<Rating>(sql, new { mediaId });
        }

        // Intern z. B. für „meine Ratings“ – liefert auch confirmed = false
        public async Task<IEnumerable<Rating>> GetByUserId(int userId)
        {
            using var db = new NpgsqlConnection(_connStr);
            const string sql = @"SELECT r.id, r.media_id AS MediaId, r.user_id AS UserId, r.stars, r.comment,
                                 r.confirmed, r.timestamp AS CreatedAt,
                                 m.title AS MediaTitle
                                 FROM   ratings r
                                 JOIN   media   m ON m.id = r.media_id
                                 WHERE  r.user_id = @userId
                                 ORDER  BY r.timestamp DESC;";
            return await db.QueryAsync<Rating>(sql, new { userId });
        }

        public async Task Update(Rating rating)
        {
            using var db = new NpgsqlConnection(_connStr);
            const string sql = @"UPDATE ratings
                                 SET stars=@Stars, comment=@Comment, confirmed=@Confirmed, timestamp=NOW()
                                 WHERE id = @Id;";
            await db.ExecuteAsync(sql, rating);
        }

        public async Task Delete(int id)
        {
            using var db = new NpgsqlConnection(_connStr);
            const string sql = "DELETE FROM ratings WHERE id = @id;";
            await db.ExecuteAsync(sql, new { id });
        }
    }
}