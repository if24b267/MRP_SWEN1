using Dapper;
using MRP_SWEN1.Models;
using Npgsql;

namespace MRP_SWEN1.Repositories
{
    public class PostgreSqlRatingRepository : IRatingRepository
    {
        private readonly string _connStr;

        public PostgreSqlRatingRepository(string connStr) => _connStr = connStr;

        // Create a new rating entry
        // Returns the generated id and updates the rating object
        public async Task<int> Create(Rating rating)
        {
            using var db = new NpgsqlConnection(_connStr);

            Console.WriteLine($"[REPO] INSERT PARAMS: MediaId={rating.MediaId}, UserId={rating.UserId}, Stars={rating.Stars}, Comment={rating.Comment ?? "null"}, Confirmed={rating.Confirmed}");

            const string sql = @"
                    INSERT INTO ratings (media_id, user_id, stars, comment, confirmed)
                    VALUES (@MediaId, @UserId, @Stars, @Comment, @Confirmed)
                    RETURNING id;
                ";

            var param = new
            {
                MediaId = rating.MediaId,
                UserId = rating.UserId,
                Stars = rating.Stars,
                Comment = rating.Comment,
                Confirmed = rating.Confirmed
            };

            Console.WriteLine($"[REPO] SQL: {sql}");
            Console.WriteLine($"[REPO] PARAMS: MediaId={param.MediaId}, UserId={param.UserId}, Stars={param.Stars}, Comment={param.Comment ?? "null"}, Confirmed={param.Confirmed}");

            var id = await db.QuerySingleAsync<int>(sql, param);
            Console.WriteLine($"[REPO] RETURNED ID: {id}");
            return id;
        }

        // Load a single rating by its id
        // Returns null if not found
        public async Task<Rating?> GetById(int id)
        {
            using var db = new NpgsqlConnection(_connStr);

            const string sql = @"
                SELECT id,
                       media_id      AS MediaId,
                       user_id       AS UserId,
                       stars         AS Stars,
                       comment       AS Comment,
                       confirmed     AS Confirmed,
                       timestamp     AS Timestamp
                FROM   ratings
                WHERE  id = @id;
            ";

            return await db.QuerySingleOrDefaultAsync<Rating>(sql, new { id });
        }

        // Get all confirmed ratings for a specific media
        // Used for public display (only confirmed ratings)
        // Get all ratings for a specific media (public display)
        public async Task<IEnumerable<Rating>> GetByMediaId(int mediaId)
        {
            using var db = new NpgsqlConnection(_connStr);

            const string sql = @"
                    SELECT r.id,
                           r.media_id AS MediaId,
                           r.user_id AS UserId,
                           r.stars,
                           CASE WHEN r.confirmed THEN r.comment ELSE NULL END AS Comment,
                           r.confirmed,
                           r.timestamp AS CreatedAt,
                           u.username
                    FROM   ratings r
                    JOIN   users   u ON u.id = r.user_id
                    WHERE  r.media_id = @mediaId
                    ORDER  BY r.timestamp DESC;
            ";

            return await db.QueryAsync<Rating>(sql, new { mediaId });
        }

        // Get all ratings by a specific user
        // Includes unconfirmed ratings, e.g., for "My Ratings" view
        public async Task<IEnumerable<Rating>> GetByUserId(int userId)
        {
            using var db = new NpgsqlConnection(_connStr);

            const string sql = @"
                SELECT r.id, r.media_id AS MediaId, r.user_id AS UserId, r.stars, r.comment,
                       r.confirmed, r.timestamp AS CreatedAt,
                       m.title AS MediaTitle
                FROM   ratings r
                JOIN   media   m ON m.id = r.media_id
                WHERE  r.user_id = @userId
                ORDER  BY r.timestamp DESC;
            ";

            return await db.QueryAsync<Rating>(sql, new { userId });
        }

        // Update an existing rating
        // Resets the timestamp to NOW()
        public async Task Update(Rating rating)
        {
            using var db = new NpgsqlConnection(_connStr);

            const string sql = @"
                UPDATE ratings
                SET stars = @Stars,
                    comment = @Comment,
                    confirmed = @Confirmed,
                    timestamp = NOW()
                WHERE id = @Id;
            ";

            await db.ExecuteAsync(sql, rating);
        }

        // Delete a rating by id
        public async Task Delete(int id)
        {
            using var db = new NpgsqlConnection(_connStr);
            const string sql = "DELETE FROM ratings WHERE id = @id;";
            await db.ExecuteAsync(sql, new { id });
        }
    }
}
