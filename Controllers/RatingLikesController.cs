using Dapper;
using MRP_SWEN1.Auth;
using MRP_SWEN1.Services;
using Npgsql;
using System.Threading.Tasks;

namespace MRP_SWEN1.Controllers
{
    public class RatingLikesController
    {
        private readonly AuthService _auth;
        private readonly string _connStr;
        public RatingLikesController(AuthService auth, string connStr) => (_auth, _connStr) = (auth, connStr);

        public async Task HandleLike(RoutingRequest rr)
        {
            var authHeader = rr.Request.Headers["Authorization"];
            if (!_auth.TryAuthenticate(authHeader, out var info)) { rr.Response.StatusCode = 401; return; }

            var idStr = rr.RouteParams["id"];
            if (!int.TryParse(idStr, out var ratingId)) { rr.Response.StatusCode = 400; await HttpServer.WriteResponse(rr.Response, new { error = "invalid id" }); return; }

            using var db = new NpgsqlConnection(_connStr);
            const string sql = "INSERT INTO rating_likes (rating_id, user_id) VALUES (@rid, @uid) ON CONFLICT DO NOTHING;";
            await db.ExecuteAsync(sql, new { rid = ratingId, uid = info.UserId });
            await HttpServer.WriteResponse(rr.Response, new { message = "liked" });
        }

        public async Task HandleUnlike(RoutingRequest rr)
        {
            var authHeader = rr.Request.Headers["Authorization"];
            if (!_auth.TryAuthenticate(authHeader, out var info)) { rr.Response.StatusCode = 401; return; }

            var idStr = rr.RouteParams["id"];
            if (!int.TryParse(idStr, out var ratingId)) { rr.Response.StatusCode = 400; await HttpServer.WriteResponse(rr.Response, new { error = "invalid id" }); return; }

            using var db = new NpgsqlConnection(_connStr);
            await db.ExecuteAsync("DELETE FROM rating_likes WHERE rating_id = @rid AND user_id = @uid;", new { rid = ratingId, uid = info.UserId });
            await HttpServer.WriteResponse(rr.Response, new { message = "unliked" });
        }
    }
}