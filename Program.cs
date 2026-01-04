using MRP_SWEN1.Auth;
using MRP_SWEN1.Controllers;
using MRP_SWEN1.Repositories;
using MRP_SWEN1.Services;

namespace MRP_SWEN1
{
    public class Program
    {
        static void Main(string[] args)
        {
            int port = Environment.GetEnvironmentVariable("MRP_PORT") is string p && int.TryParse(p, out var pp) ? pp : 8080;

            Console.WriteLine($"Starting MRP HTTP server on http://+:{port}/api/");
            var server = new HttpServer(prefix: $"http://+:{port}/api/");

            // PostgreSQL connection string
            const string connStr = "Host=localhost;Database=mrp_db;Username=mrp_user;Password=mrp_pass";

            // Repositories
            var userRepo = new PostgreSqlUserRepository(connStr);
            var mediaRepo = new PostgreSqlMediaRepository(connStr);
            var ratingRepo = new PostgreSqlRatingRepository(connStr);

            // Auth
            var tokenStore = new TokenStore();
            var authService = new AuthService(userRepo, tokenStore);

            // Controllers
            var usersController = new UsersController(userRepo, authService, tokenStore);
            var mediaController = new MediaController(mediaRepo, ratingRepo, authService, tokenStore, connStr);
            var favController = new FavoritesController(authService, connStr);
            var statsController = new StatisticsController(authService, connStr);
            var recController = new RecommendationsController(authService, connStr);
            var likeController = new RatingLikesController(authService, connStr);

            // Register routes
            server.StartWithControllers(
                usersController,
                mediaController,
                favController,
                statsController,
                recController,
                likeController
            );

            Console.WriteLine("Press Ctrl+C to stop.\n");
            var exitEvent = new ManualResetEvent(false);
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                server.Stop();
                exitEvent.Set();
            };
            exitEvent.WaitOne();
        }
    }
}