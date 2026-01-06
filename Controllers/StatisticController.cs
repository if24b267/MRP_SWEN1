using Dapper;
using MRP_SWEN1.Services;
using Npgsql;

namespace MRP_SWEN1.Controllers
{
    public class StatisticsController
    {
        private readonly AuthService _auth;
        private readonly string _connStr;

        // Controller for user statistics and leaderboard data
        public StatisticsController(AuthService auth, string connStr)
        {
            _auth = auth;
            _connStr = connStr;
        }

        // GET /api/statistics/profile
        // Returns personal statistics for the authenticated user
        public async Task HandleProfileStats(RoutingRequest rr)
        {
            // Read and verify Authorization header
            var authHeader = rr.Request.Headers["Authorization"] ?? "";
            if (!_auth.TryAuthenticate(authHeader, out var info))
            {
                rr.Response.StatusCode = 401;
                return;
            }

            // Personal statistics include:
            // - total number of ratings written by the user
            // - average star rating given
            // - favorite genre as stored in the user profile
            using var db = new NpgsqlConnection(_connStr);

            const string sql = @"
                SELECT COUNT(*) AS total_ratings,
                       AVG(stars) AS average_stars,
                       MAX(u.favorite_genre) AS favorite_genre
                FROM ratings r
                JOIN users   u ON u.id = r.user_id
                WHERE r.user_id = @uid;
            ";

            // Query statistics for the current user
            var stats = await db.QuerySingleOrDefaultAsync(
                sql,
                new { uid = info.UserId }
            );

            // If the user has no ratings yet, return default values
            await HttpServer.WriteResponse(
                rr.Response,
                stats ?? new
                {
                    total_ratings = 0,
                    average_stars = (object?)null,
                    favorite_genre = (object?)null
                }
            );
        }

        // GET /api/statistics/leaderboard
        // Returns the top 10 most active reviewers
        public async Task HandleLeaderboard(RoutingRequest rr)
        {
            // Leaderboard is ranked by total number of ratings per user
            using var db = new NpgsqlConnection(_connStr);

            const string sql = @"
                SELECT username, COUNT(*) AS rating_count
                FROM ratings
                JOIN users ON users.id = ratings.user_id
                GROUP BY username
                ORDER BY rating_count DESC
                LIMIT 10;
            ";

            // Query leaderboard data
            var board = await db.QueryAsync(sql);
            await HttpServer.WriteResponse(rr.Response, board);
        }
    }
}
