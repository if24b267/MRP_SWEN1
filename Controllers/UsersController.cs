using MRP_SWEN1.Auth;
using MRP_SWEN1.Repositories;
using MRP_SWEN1.Services;
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

        public UsersController(IUserRepository userRepo, AuthService auth, TokenStore tokenStore)
        {
            _userRepo = userRepo;
            _auth = auth;
            _tokenStore = tokenStore;
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
        // Returns id, username, email (nullable), favoriteGenre (nullable).
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
    }
}
