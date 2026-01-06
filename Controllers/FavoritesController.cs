using Dapper;
using MRP_SWEN1.Models;
using MRP_SWEN1.Services;
using Npgsql;

namespace MRP_SWEN1.Controllers
{
    // Controller responsible for managing user favorites (toggle & list).
    public class FavoritesController
    {
        private readonly AuthService _auth;
        private readonly string _connStr;

        // AuthService is injected for token validation,
        // connection string is used for direct PostgreSQL access.
        public FavoritesController(AuthService auth, string connStr) => (_auth, _connStr) = (auth, connStr);

        // Adds or removes a media entry from the user's favorites.
        public async Task HandleToggle(RoutingRequest rr)
        {
            // Authenticate request via Authorization header
            var authHeader = rr.Request.Headers["Authorization"] ?? "";
            if (!_auth.TryAuthenticate(authHeader, out var info)) 
            { 
                rr.Response.StatusCode = 401; 
                return; 
            }

            // Validate media id from route parameters
            var idStr = rr.RouteParams["id"];
            if (!int.TryParse(idStr, out var mediaId)) 
            { 
                rr.Response.StatusCode = 400; 
                await HttpServer.WriteResponse(rr.Response, new { error = "invalid id" }); 
                return; 
            }

            using var db = new NpgsqlConnection(_connStr);

            // Check whether the media is already marked as favorite
            const string exists = "SELECT 1 FROM favorites WHERE user_id = @uid AND media_id = @mid;";
            bool isFav = await 
                db.QuerySingleOrDefaultAsync<int>(exists, new { uid = info.UserId, mid = mediaId }) != 0;

            // Toggle favorite state
            if (isFav)
            {
                await db.ExecuteAsync("DELETE FROM favorites WHERE user_id = @uid AND media_id = @mid;", 
                    new { uid = info.UserId, mid = mediaId });
            }
            else
            {
                await db.ExecuteAsync("INSERT INTO favorites (user_id, media_id) VALUES (@uid, @mid);", 
                    new { uid = info.UserId, mid = mediaId });
            }

            await HttpServer.WriteResponse(rr.Response, new { message = isFav ? "unfavorited" : "favorited" });
        }

        // GET /api/favorites
        // Returns all favorite media entries of the authenticated user.
        public async Task HandleList(RoutingRequest rr)
        {
            // Authenticate request
            var authHeader = rr.Request.Headers["Authorization"] ?? "";
            if (!_auth.TryAuthenticate(authHeader, out var info))
            {
                rr.Response.StatusCode = 401;
                return;
            }

            using var db = new NpgsqlConnection(_connStr);

            const string sql = @"SELECT m.id,
                                       m.title,
                                       m.description,
                                       m.media_type,
                                       m.release_year,
                                       m.genres,
                                       m.age_restriction,
                                       m.creator_user_id
                                 FROM   media m
                                 JOIN   favorites f ON f.media_id = m.id
                                 WHERE  f.user_id = @uid;
            ";

            var rows = await db.QueryAsync(sql, new { uid = info.UserId });

            var list = rows.Select(row => new MediaEntry
            {
                Id = row.id,
                Title = row.title,
                Description = row.description,
                MediaType = row.media_type,
                ReleaseYear = row.release_year,
                Genres = ((string[])row.genres)?.ToList() ?? new List<string>(),
                AgeRestriction = row.age_restriction,
                CreatorUserId = row.creator_user_id
            });

            await HttpServer.WriteResponse(rr.Response, list);
        }
    }
}