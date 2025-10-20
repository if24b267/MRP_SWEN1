using MRP_SWEN1.Auth;
using MRP_SWEN1.Controllers;
using MRP_SWEN1.Repositories;
using MRP_SWEN1.Services;

namespace MRP_SWEN1
{
    // Program entry point. For the intermediate hand-in we run the in-memory server.
    // This keeps the project easy to run (no DB setup).
    public class Program
    {
        static void Main(string[] args)
        {
            // Check if an environment variable "MRP_PORT" exists --> use it as the port number
            // If not (or if it is invalid), just use 8080 as the default port. Basically: lets us change the port without editing the code.
            int port = Environment.GetEnvironmentVariable("MRP_PORT") is string p && int.TryParse(p, out var pp) ? pp : 8080;

            Console.WriteLine($"Starting MRP HTTP server on http://localhost:{port}/api/");
            var server = new HttpServer(prefix: $"http://+:{port}/api/");

            Console.WriteLine("Running in IN-MEMORY mode for the Intermediate hand-in.");
            StartWithInMemory(server);

            Console.WriteLine("Press Ctrl+C to stop.\n");

            // Create a "manual reset event" that acts like a signal or gate.
            // false = "the gate" is closed at first (the program will wait until we open it)
            var exitEvent = new System.Threading.ManualResetEvent(false);

            // When the user presses CTRL + C in the console:
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;          // Stop the program from instantly closing
                server.Stop();            // Stop the running server "gracefully"
                exitEvent.Set();          // Open the gate --> allow the program to continue and exit
            };

            // Wait here until the signal (exitEvent.Set()) is triggered.
            // This basically keeps the program running until we press CTRL + C.
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
