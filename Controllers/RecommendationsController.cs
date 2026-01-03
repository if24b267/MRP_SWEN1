using Dapper;
using MRP_SWEN1.Auth;
using MRP_SWEN1.Models;
using MRP_SWEN1.Services;
using Npgsql;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MRP_SWEN1.Controllers
{
    public class RecommendationsController
    {
        private readonly AuthService _auth;
        private readonly string _connStr;
        public RecommendationsController(AuthService auth, string connStr) => (_auth, _connStr) = (auth, connStr);

        public async Task HandleRecommendations(RoutingRequest rr)
        {
            var authHeader = rr.Request.Headers["Authorization"];
            if (!_auth.TryAuthenticate(authHeader, out var info)) { rr.Response.StatusCode = 401; return; }

            using var db = new NpgsqlConnection(_connStr);
            // Top-Genre des Users finden
            const string genreSql = @"SELECT UNNEST(m.genres) AS g, AVG(r.stars) AS s
                                      FROM ratings r
                                      JOIN media m ON m.id = r.media_id
                                      WHERE r.user_id = @uid
                                      GROUP BY g
                                      ORDER BY s DESC
                                      LIMIT 1;";
            var topGenre = await db.QuerySingleOrDefaultAsync<string>(genreSql, new { uid = info.UserId });

            if (topGenre == null)
            {
                await HttpServer.WriteResponse(rr.Response, new List<MediaEntry>());
                return;
            }

            const string recSql = @"SELECT DISTINCT m.*
                                    FROM media m
                                    WHERE @topGenre = ANY(m.genres)
                                      AND m.id NOT IN (
                                          SELECT media_id FROM ratings WHERE user_id = @uid
                                      )
                                    ORDER BY m.release_year DESC
                                    LIMIT 10;";
            var list = await db.QueryAsync<MediaEntry>(recSql, new { uid = info.UserId, topGenre });
            await HttpServer.WriteResponse(rr.Response, list);
        }
    }
}