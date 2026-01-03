using Dapper;
using MRP_SWEN1.Auth;
using MRP_SWEN1.Models;
using MRP_SWEN1.Repositories;
using MRP_SWEN1.Services;
using Npgsql;
using System.Threading.Tasks;

namespace MRP_SWEN1.Controllers
{
    public class FavoritesController
    {
        private readonly AuthService _auth;
        private readonly string _connStr;
        public FavoritesController(AuthService auth, string connStr) => (_auth, _connStr) = (auth, connStr);

        public async Task HandleToggle(RoutingRequest rr)
        {
            var authHeader = rr.Request.Headers["Authorization"] ?? "";
            if (!_auth.TryAuthenticate(authHeader, out var info)) { rr.Response.StatusCode = 401; return; }

            var idStr = rr.RouteParams["id"];
            if (!int.TryParse(idStr, out var mediaId)) { rr.Response.StatusCode = 400; await HttpServer.WriteResponse(rr.Response, new { error = "invalid id" }); return; }

            using var db = new NpgsqlConnection(_connStr);
            const string exists = "SELECT 1 FROM favorites WHERE user_id = @uid AND media_id = @mid;";
            var isFav = await db.QuerySingleOrDefaultAsync<int>(exists, new { uid = info.UserId, mid = mediaId }) != 0;

            if (isFav)
                await db.ExecuteAsync("DELETE FROM favorites WHERE user_id = @uid AND media_id = @mid;", new { uid = info.UserId, mid = mediaId });
            else
                await db.ExecuteAsync("INSERT INTO favorites (user_id, media_id) VALUES (@uid, @mid);", new { uid = info.UserId, mid = mediaId });

            await HttpServer.WriteResponse(rr.Response, new { message = isFav ? "unfavorited" : "favorited" });
        }

        public async Task HandleList(RoutingRequest rr)
        {
            var authHeader = rr.Request.Headers["Authorization"] ?? "";
            if (!_auth.TryAuthenticate(authHeader, out var info)) { rr.Response.StatusCode = 401; return; }

            using var db = new NpgsqlConnection(_connStr);
            const string sql = @"SELECT m.* FROM media m
                                 JOIN favorites f ON f.media_id = m.id
                                 WHERE f.user_id = @uid;";
            var list = await db.QueryAsync<MediaEntry>(sql, new { uid = info.UserId });
            await HttpServer.WriteResponse(rr.Response, list);
        }
    }
}