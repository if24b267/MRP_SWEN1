using System.Net;
using System.Text;
using MRP_SWEN1.Controllers;

namespace MRP_SWEN1
{
    // Very small HTTP server wrapper using HttpListener.
    // Purpose: keep demo minimal without bringing ASP.NET Core.
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
        public void StartWithControllers(UsersController usersController, MediaController mediaController)
        {
            // Register routes
            _router.Register("POST", "/api/users/register", usersController.HandleRegister);
            _router.Register("POST", "/api/users/login", usersController.HandleLogin);
            _router.Register("GET", "/api/users/{username}/profile", usersController.HandleGetProfile);

            _router.Register("POST", "/api/media", mediaController.HandleCreate);
            _router.Register("GET", "/api/media", mediaController.HandleSearch);
            _router.Register("GET", "/api/media/{id}", mediaController.HandleGetById);
            _router.Register("PUT", "/api/media/{id}", mediaController.HandleUpdate);
            _router.Register("DELETE", "/api/media/{id}", mediaController.HandleDelete);

            // Ratings routes
            _router.Register("POST", "/api/media/{id}/rate", mediaController.HandleRate);
            _router.Register("GET", "/api/media/{id}/ratings", mediaController.HandleGetRatingsForMedia);
            _router.Register("PUT", "/api/ratings/{id}", mediaController.HandleUpdateRating);
            _router.Register("DELETE", "/api/ratings/{id}", mediaController.HandleDeleteRating);

            // Start listener
            _listener.Start();
            _running = true;
            Task.Run(() => ListenLoop());
            Console.WriteLine($"HTTP server started at {_prefix}");
        }

        // ListenLoop accepts requests and dispatches them to the Router/handlers.
        // Each request is processed in a background Task to allow concurrency.
        private async Task ListenLoop()
        {
            while (_running)
            {
                var ctx = await _listener.GetContextAsync().ConfigureAwait(false);
                _ = Task.Run(() => ProcessContext(ctx));
            }
        }

        public void Stop()
        {
            _running = false;
            try { _listener.Stop(); } catch { }
            Console.WriteLine("Server stopped.");
        }

        // Maps incoming HttpListenerContext to our RoutingRequest object and executes handler.
        private async Task ProcessContext(HttpListenerContext ctx)
        {
            try
            {
                var req = ctx.Request;
                var res = ctx.Response;
                Console.WriteLine($"{DateTime.Now:O} {req.HttpMethod} {req.Url.PathAndQuery}");

                var handler = _router.Match(req.HttpMethod, req.Url.AbsolutePath, out var routeParams);
                if (handler == null)
                {
                    res.StatusCode = 404;
                    await WriteResponse(res, new { error = "Not Found" });
                    return;
                }

                var requestData = new RoutingRequest
                {
                    Request = req,
                    Response = res,
                    RouteParams = routeParams
                };

                await handler(requestData);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unhandled exception: " + ex);
                try
                {
                    ctx.Response.StatusCode = 500;
                    await WriteResponse(ctx.Response, new { error = "Server error" });
                }
                catch { }
            }
        }

        // Utility to write an object as JSON response.
        public static async Task WriteResponse(HttpListenerResponse response, object obj)
        {
            response.ContentType = "application/json";
            var json = System.Text.Json.JsonSerializer.Serialize(obj);
            var buffer = Encoding.UTF8.GetBytes(json);
            response.ContentLength64 = buffer.Length;
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            response.OutputStream.Close();
        }
    }
}
