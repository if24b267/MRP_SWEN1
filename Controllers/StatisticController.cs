using Dapper;
using MRP_SWEN1.Auth;
using MRP_SWEN1.Services;
using Npgsql;
using System.Threading.Tasks;

namespace MRP_SWEN1.Controllers
{
    public class StatisticsController
    {
        private readonly AuthService _auth;
        private readonly string _connStr;
        public StatisticsController(AuthService auth, string connStr) => (_auth, _connStr) = (auth, connStr);

        public async Task HandleProfileStats(RoutingRequest rr)
        {
            var authHeader = rr.Request.Headers["Authorization"];
            if (!_auth.TryAuthenticate(authHeader, out var info)) { rr.Response.StatusCode = 401; return; }

            using var db = new NpgsqlConnection(_connStr);
            const string sql = @"SELECT COUNT(*) AS total_ratings,
                                        AVG(stars) AS average_stars,
                                        favorite_genre
                                 FROM ratings
                                 JOIN users ON users.id = ratings.user_id
                                 WHERE user_id = @uid
                                 GROUP BY favorite_genre;";
            var stats = await db.QuerySingleOrDefaultAsync(sql, new { uid = info.UserId });
            await HttpServer.WriteResponse(rr.Response, stats ?? new { total_ratings = 0, average_stars = (object?)null, favorite_genre = (object?)null });
        }

        public async Task HandleLeaderboard(RoutingRequest rr)
        {
            using var db = new NpgsqlConnection(_connStr);
            const string sql = @"SELECT username, COUNT(*) AS rating_count
                                 FROM ratings
                                 JOIN users ON users.id = ratings.user_id
                                 GROUP BY username
                                 ORDER BY rating_count DESC
                                 LIMIT 10;";
            var board = await db.QueryAsync(sql);
            await HttpServer.WriteResponse(rr.Response, board);
        }
    }
}