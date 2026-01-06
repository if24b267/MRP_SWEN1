using Dapper;
using MRP_SWEN1.Models;
using MRP_SWEN1.Services;
using Npgsql;

namespace MRP_SWEN1.Controllers
{
    public class RecommendationsController
    {
        private readonly AuthService _auth;
        private readonly string _connStr;

        // Controller for generating simple media recommendations
        public RecommendationsController(AuthService auth, string connStr)
            => (_auth, _connStr) = (auth, connStr);

        // GET /api/recommendations
        // Returns media recommendations based on the user's past ratings
        public async Task HandleRecommendations(RoutingRequest rr)
        {
            // Read and verify Authorization header
            var authHeader = rr.Request.Headers["Authorization"] ?? "";
            if (!_auth.TryAuthenticate(authHeader, out var info))
            {
                rr.Response.StatusCode = 401;
                return;
            }

            using var db = new NpgsqlConnection(_connStr);

            // Determine the user's top genre based on their average ratings
            // The genre with the highest average star rating is selected
            const string genreSql = @"
                SELECT UNNEST(m.genres) AS g, AVG(r.stars) AS s
                FROM ratings r
                JOIN media m ON m.id = r.media_id
                WHERE r.user_id = @uid
                GROUP BY g
                ORDER BY s DESC
                LIMIT 1;
            ";

            var topGenre = await db.QuerySingleOrDefaultAsync<string>(
                genreSql,
                new { uid = info.UserId }
            );

            // If the user has no ratings yet, no recommendations can be generated
            if (topGenre == null)
            {
                await HttpServer.WriteResponse(rr.Response, new List<MediaEntry>());
                return;
            }

            // Select up to 10 media entries from the top genre
            // Exclude media that the user has already rated
            const string recSql = @"
                SELECT DISTINCT m.*
                FROM media m
                WHERE @topGenre = ANY(m.genres)
                  AND m.id NOT IN (
                      SELECT media_id
                      FROM ratings
                      WHERE user_id = @uid
                  )
                ORDER BY m.release_year DESC
                LIMIT 10;
            ";

            var rows = await db.QueryAsync(recSql, new { uid = info.UserId, topGenre });
            var list = rows.Select(r => new MediaEntry
            {
                Id = r.id,
                Title = r.title,
                Description = r.description,
                MediaType = r.media_type,
                ReleaseYear = r.release_year,
                Genres = ((string[])r.genres)?.ToList() ?? new List<string>(),
                AgeRestriction = r.age_restriction,
                CreatorUserId = r.creator_user_id
            });

            // Return recommendation list
            await HttpServer.WriteResponse(rr.Response, list);
        }
    }
}
