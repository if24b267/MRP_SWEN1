using MRP_SWEN1.Auth;
using MRP_SWEN1.Controllers;
using MRP_SWEN1.Repositories;
using MRP_SWEN1.Services;

namespace MRP_SWEN1
{
    // Program entry point. For the intermediate hand-in we always run the in-memory server.
    // This keeps the project easy to run (no DB setup).
    public class Program
    {
        static void Main(string[] args)
        {
            int port = Environment.GetEnvironmentVariable("MRP_PORT") is string p && int.TryParse(p, out var pp) ? pp : 8080;

            Console.WriteLine($"Starting MRP HTTP server on http://localhost:{port}/api/");
            var server = new HttpServer(prefix: $"http://+:{port}/api/");

            Console.WriteLine("Running in IN-MEMORY mode for the Intermediate hand-in.");
            StartWithInMemory(server);

            Console.WriteLine("Press Ctrl+C to stop.");

            var exitEvent = new System.Threading.ManualResetEvent(false);
            Console.CancelKeyPress += (s, e) => { e.Cancel = true; server.Stop(); exitEvent.Set(); };
            exitEvent.WaitOne();
        }

        static void StartWithInMemory(HttpServer server)
        {
            // create in-memory repos
            var userRepo = new InMemoryUserRepository();
            var mediaRepo = new InMemoryMediaRepository();
            var ratingRepo = new InMemoryRatingRepository();

            // tokenstore + auth service
            var tokenStore = new TokenStore();
            var authService = new AuthService(userRepo, tokenStore);

            // controllers
            var usersController = new UsersController(userRepo, authService, tokenStore);
            var mediaController = new MediaController(mediaRepo, ratingRepo, authService, tokenStore);

            // register routes and start the server
            server.StartWithControllers(usersController, mediaController);
        }
    }
}
