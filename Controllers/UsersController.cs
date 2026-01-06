using Dapper;
using MRP_SWEN1.Auth;
using MRP_SWEN1.Repositories;
using MRP_SWEN1.Services;
using Npgsql;
using System.Text.Json;

namespace MRP_SWEN1.Controllers
{
    // Controller for user endpoints: register, login, get public profile.
    // Password hashing & token generation are handled in AuthService.
    public class UsersController
    {
        private readonly IUserRepository _userRepo;
        private readonly AuthService _auth;
        private readonly TokenStore _tokenStore;
        private readonly string _connStr;

        public UsersController(IUserRepository userRepo, AuthService auth, TokenStore tokenStore, string connStr)
        {
            _userRepo = userRepo;
            _auth = auth;
            _tokenStore = tokenStore;
            _connStr = connStr;
        }

        // Register a new user: expects JSON body { username, password }.
        public async Task HandleRegister(RoutingRequest rr)
        {
            var body = await new StreamReader(rr.Request.InputStream).ReadToEndAsync();
            var doc = JsonSerializer.Deserialize<JsonElement>(body);

            if (!doc.TryGetProperty("username", out var u) || !doc.TryGetProperty("password", out var p))
            {
                rr.Response.StatusCode = 400;
                await HttpServer.WriteResponse(rr.Response, new { error = "username and password required" });
                return;
            }

            var (ok, err) = await _auth.Register(u.GetString()!, p.GetString()!);
            if (!ok)
            {
                rr.Response.StatusCode = 400;
                await HttpServer.WriteResponse(rr.Response, new { error = err });
                return;
            }

            rr.Response.StatusCode = 201;
            await HttpServer.WriteResponse(rr.Response, new { message = "User created" });
        }

        // Login: returns a simple bearer token used for protected endpoints.
        public async Task HandleLogin(RoutingRequest rr)
        {
            var body = await new StreamReader(rr.Request.InputStream).ReadToEndAsync();
            var doc = JsonSerializer.Deserialize<JsonElement>(body);

            if (!doc.TryGetProperty("username", out var u) || !doc.TryGetProperty("password", out var p))
            {
                rr.Response.StatusCode = 400;
                await HttpServer.WriteResponse(rr.Response, new { error = "username and password required" });
                return;
            }

            var (ok, token, err, user) = await _auth.Login(u.GetString()!, p.GetString()!);
            if (!ok)
            {
                rr.Response.StatusCode = 401;
                await HttpServer.WriteResponse(rr.Response, new { error = err });
                return;
            }

            await HttpServer.WriteResponse(rr.Response, new { token, username = user!.Username });
        }

        // Get a user's public profile. Requires auth to view profile.
        // Returns id, username, favoriteGenre (nullable).
        public async Task HandleGetProfile(RoutingRequest rr)
        {
            // auth
            var authHeader = rr.Request.Headers["Authorization"];
            if (!_auth.TryAuthenticate(authHeader, out var info))
            {
                rr.Response.StatusCode = 401;
                await HttpServer.WriteResponse(rr.Response, new { error = "Unauthorized" });
                return;
            }

            var username = rr.RouteParams.ContainsKey("username") ? rr.RouteParams["username"] : null;
            if (username == null)
            {
                rr.Response.StatusCode = 400;
                await HttpServer.WriteResponse(rr.Response, new { error = "username missing in path" });
                return;
            }

            var user = await _userRepo.GetByUsername(username);
            if (user == null)
            {
                rr.Response.StatusCode = 404;
                await HttpServer.WriteResponse(rr.Response, new { error = "User not found" });
                return;
            }

            // public profile
            await HttpServer.WriteResponse(rr.Response, new
            {
                id = user.Id,
                username = user.Username,
                favoriteGenre = user.FavoriteGenre
            });
        }

        // PUT /api/users/{username}/profile
        public async Task HandleUpdateProfile(RoutingRequest rr)
        {
            // 1. Auth
            var authHeader = rr.Request.Headers["Authorization"] ?? "";
            if (!_auth.TryAuthenticate(authHeader, out var info))
            {
                rr.Response.StatusCode = 401;
                return;
            }

            // 2. Nur eigenes Profil dürfen bearbeiten
            var username = rr.RouteParams["username"];
            if (!username.Equals(info.Username, StringComparison.OrdinalIgnoreCase))
            {
                rr.Response.StatusCode = 403;
                await HttpServer.WriteResponse(rr.Response, new { error = "can only update own profile" });
                return;
            }

            // 3. Body einlesen
            var body = await new StreamReader(rr.Request.InputStream).ReadToEndAsync();
            var doc = JsonSerializer.Deserialize<JsonElement>(body);

            if (!doc.TryGetProperty("favoriteGenre", out var fg) ||
                string.IsNullOrWhiteSpace(fg.GetString()))
            {
                rr.Response.StatusCode = 400;
                await HttpServer.WriteResponse(rr.Response, new { error = "favoriteGenre missing or empty" });
                return;
            }

            // 4. DB-Update
            using var db = new NpgsqlConnection(_connStr);
            var rows = await db.ExecuteAsync(
                "UPDATE users SET favorite_genre = @genre WHERE id = @uid",
                new { genre = fg.GetString(), uid = info.UserId });

            if (rows == 0)
            {
                rr.Response.StatusCode = 404;
                await HttpServer.WriteResponse(rr.Response, new { error = "user not found" });
                return;
            }

            await HttpServer.WriteResponse(rr.Response, new { message = "profile updated" });
        }
    }
}
