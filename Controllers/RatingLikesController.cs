using Dapper;
using MRP_SWEN1.Services;
using Npgsql;

namespace MRP_SWEN1.Controllers
{
    // Controller for liking and unliking ratings
    public class RatingLikesController
    {
        private readonly AuthService _auth;
        private readonly string _connStr;
        public RatingLikesController(AuthService auth, string connStr)
        {
            _auth = auth;
            _connStr = connStr;
        }

        // POST /api/ratings/{id}/like
        // Allows an authenticated user to like a rating
        public async Task HandleLike(RoutingRequest rr)
        {
            // Read and verify Authorization header
            var authHeader = rr.Request.Headers["Authorization"] ?? "";
            if (!_auth.TryAuthenticate(authHeader, out var info))
            {
                rr.Response.StatusCode = 401;
                return;
            }

            // Read rating id from route and validate it
            var idStr = rr.RouteParams["id"];
            if (!int.TryParse(idStr, out var ratingId))
            {
                rr.Response.StatusCode = 400;
                await HttpServer.WriteResponse(rr.Response, new { error = "invalid id" });
                return;
            }

            // Insert like for this rating
            // ON CONFLICT DO NOTHING prevents duplicate likes by the same user
            using var db = new NpgsqlConnection(_connStr);
            const string sql =
                "INSERT INTO rating_likes (rating_id, user_id) " +
                "VALUES (@rid, @uid) ON CONFLICT DO NOTHING;";
            await db.ExecuteAsync(sql, new { rid = ratingId, uid = info.UserId });

            await HttpServer.WriteResponse(rr.Response, new { message = "liked" });
        }

        // DELETE /api/ratings/{id}/like
        // Allows an authenticated user to remove their like
        public async Task HandleUnlike(RoutingRequest rr)
        {
            // Read and verify Authorization header
            var authHeader = rr.Request.Headers["Authorization"] ?? "";
            if (!_auth.TryAuthenticate(authHeader, out var info))
            {
                rr.Response.StatusCode = 401;
                return;
            }

            // Read rating id from route and validate it
            var idStr = rr.RouteParams["id"];
            if (!int.TryParse(idStr, out var ratingId))
            {
                rr.Response.StatusCode = 400;
                await HttpServer.WriteResponse(rr.Response, new { error = "invalid id" });
                return;
            }

            // Remove like for this rating and user
            using var db = new NpgsqlConnection(_connStr);
            await db.ExecuteAsync(
                "DELETE FROM rating_likes WHERE rating_id = @rid AND user_id = @uid;",
                new { rid = ratingId, uid = info.UserId }
            );

            await HttpServer.WriteResponse(rr.Response, new { message = "unliked" });
        }
    }
}