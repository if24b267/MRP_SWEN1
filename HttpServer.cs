using System.Net;
using System.Text;
using MRP_SWEN1.Controllers;

namespace MRP_SWEN1
{
    // Very small HTTP server wrapper using HttpListener.
    // StartWithControllers registers routes and starts a background listen loop.
    public class HttpServer
    {
        private readonly HttpListener _listener;
        private readonly Router _router;
        private readonly string _prefix;
        private bool _running = false;

        public HttpServer(string prefix)
        {
            _prefix = prefix;
            _listener = new HttpListener();
            _listener.Prefixes.Add(prefix);
            _router = new Router();
        }

        // Start only with already-created controllers (In-Memory mode)
        public void StartWithControllers(
            UsersController uc,
            MediaController mc,
            FavoritesController fc,
            StatisticsController sc,
            RecommendationsController rc,
            RatingLikesController lc)
        {
            // User
            _router.Register("POST", "/api/users/register", uc.HandleRegister);
            _router.Register("POST", "/api/users/login", uc.HandleLogin);
            _router.Register("GET", "/api/users/{username}/profile", uc.HandleGetProfile);

            // Media
            _router.Register("POST", "/api/media", mc.HandleCreate);
            _router.Register("GET", "/api/media", mc.HandleSearch);
            _router.Register("GET", "/api/media/{id}", mc.HandleGetById);
            _router.Register("PUT", "/api/media/{id}", mc.HandleUpdate);
            _router.Register("DELETE", "/api/media/{id}", mc.HandleDelete);

            // Ratings
            _router.Register("POST", "/api/media/{id}/rate", mc.HandleRate);
            _router.Register("GET", "/api/media/{id}/ratings", mc.HandleGetRatingsForMedia);
            _router.Register("PUT", "/api/ratings/{id}", mc.HandleUpdateRating);
            _router.Register("DELETE", "/api/ratings/{id}", mc.HandleDeleteRating);
            _router.Register("GET", "/api/ratings/mine", mc.HandleGetMyRatings);

            // Favorites
            _router.Register("POST", "/api/media/{id}/favorite", fc.HandleToggle);
            _router.Register("DELETE", "/api/media/{id}/favorite", fc.HandleToggle);
            _router.Register("GET", "/api/favorites", fc.HandleList);

            // Statistics
            _router.Register("GET", "/api/users/{username}/stats", sc.HandleProfileStats);
            _router.Register("GET", "/api/leaderboard", sc.HandleLeaderboard);

            // Recommendations
            _router.Register("GET", "/api/recommendations", rc.HandleRecommendations);

            // Rating Likes
            _router.Register("POST", "/api/ratings/{id}/like", lc.HandleLike);
            _router.Register("DELETE", "/api/ratings/{id}/like", lc.HandleUnlike);

            _listener.Start();
            _running = true;
            Task.Run(() => ListenLoop());
            Console.WriteLine($"HTTP server started at {_prefix}");
        }

        // ListenLoop listens / accepts requests and dispatches them to the Router/handlers.
        // Each request is processed in a background Task to allow concurrency.
        private async Task ListenLoop()
        {
            // Run this loop as long as the server is marked as "running"
            while (_running)
            {
                // Wait (asynchronously) until a client connects and sends a request.
                // This line "pauses" here until something comes in.
                var context = await _listener.GetContextAsync().ConfigureAwait(false);

                // Handle each request in a separate background task
                // so the server can immediately continue listening for the next one.
                _ = Task.Run(() => ProcessContext(context));
            }
        }


        public void Stop()
        {
            _running = false;
            try { _listener.Stop(); } catch { }
            Console.WriteLine("Server stopped.");
        }

        // Maps incoming HttpListenerContext to our RoutingRequest object and executes handler.
        private async Task ProcessContext(HttpListenerContext context)
        {
            try
            {
                var req = context.Request;
                var res = context.Response;

                // "log" each incoming request (time, method, path)
                Console.WriteLine($"{DateTime.Now:O} {req.HttpMethod} {req.Url.PathAndQuery}");

                // try to match the incoming request to a registered route
                // routeParams will contain values extracted from path placeholders, like {id}
                var handler = _router.Match(req.HttpMethod, req.Url.AbsolutePath, out var routeParams);
                if (handler == null)
                {
                    // no matching route found
                    res.StatusCode = 404;
                    await WriteResponse(res, new { error = "Not Found" });
                    return;
                }

                // wrap HttpListener request/response + route parameters into RoutingRequest
                var requestData = new RoutingRequest
                {
                    Request = req,
                    Response = res,
                    RouteParams = routeParams
                };

                // call the handler for this route (for example controller method)
                await handler(requestData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
                // catch any unexpected errors in the request handling
                Console.WriteLine("Unhandled exception: " + ex);
                try
                {
                    context.Response.StatusCode = 500; // internal server error
                    await WriteResponse(context.Response, new { error = "Server error" });
                }
                catch { } // ignore if sending error fails
            }
        }

        // Utility to write a response as JSON
        public static async Task WriteResponse(HttpListenerResponse response, object obj)
        {
            response.ContentType = "application/json"; // tell client it is JSON

            // serialize object to JSON string
            var json = System.Text.Json.JsonSerializer.Serialize(obj);

            // convert JSON string to bytes
            var buffer = Encoding.UTF8.GetBytes(json);

            // set Content-Length header
            response.ContentLength64 = buffer.Length;

            // write the bytes to the response stream asynchronously
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);

            // close the output stream (required for HttpListener to send response)
            response.OutputStream.Close();
        }
    }
}
