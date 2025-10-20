using System.Text.RegularExpressions;
using System.Net;

namespace MRP_SWEN1
{
    // Represents one HTTP request + response + any extracted route parameters.
    public class RoutingRequest
    {
        public HttpListenerRequest Request { get; set; }           // The incoming HTTP request
        public HttpListenerResponse Response { get; set; }         // The outgoing HTTP response
        public Dictionary<string, string> RouteParams { get; set; } = new();  // Holds {parameterName -> value}
    }

    // Each route handler is an async function that receives a RoutingRequest.
    public delegate Task RouteHandler(RoutingRequest req);

    public class Router
    {
        // Stores one route: HTTP method, compiled regex, parameter names and its handler.
        private record RouteEntry(string Method, Regex Regex, List<string> ParamNames, RouteHandler Handler);

        // List of all registered routes
        private readonly List<RouteEntry> _routes = new();

        // Register a new route, for example:
        // Register("GET", "/api/users/{username}", HandleUser)
        public void Register(string method, string pathPattern, RouteHandler handler)
        {
            // Store all found parameter names like {username}, {id}, ...
            var paramNames = new List<string>();

            // Convert the route pattern into a regular expression
            // Example: "/api/users/{username}" -> "^/api/users/([^/]+)$"
            // Each {param} becomes "([^/]+)" which captures one path segment
            var pattern = "^" + Regex.Replace(pathPattern, @"\{([^}]+)\}", m => {
                // Save the name inside { } (for example: "username")
                paramNames.Add(m.Groups[1].Value);

                // Replace it with a capture group for that part of the path.
                // A capture group means a part of the regex that "captures" text to read it later.
                // So basically, it grabs whatever matches that part of the URL (like "john123")
                return "([^/]+)";
            }) + "$";

            // Create a compiled, case-insensitive regex
            var regex = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);

            // Store the new route (converted to uppercase for consistency)
            _routes.Add(new RouteEntry(method.ToUpper(), regex, paramNames, handler));
        }

        // Try to find a matching route for an incoming request (method + path)
        // and extract any parameters like {username}.
        public RouteHandler Match(string method, string path, out Dictionary<string, string> routeParams)
        {
            routeParams = new();

            // Loop through all registered routes
            foreach (var r in _routes)
            {
                // Skip if HTTP method (GET, POST, etc.) does not match
                if (!string.Equals(r.Method, method, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Try to match the path with the regex for this route
                var m = r.Regex.Match(path);
                if (!m.Success)
                {
                    continue;
                }

                // If it matches, extract all parameter values
                for (int i = 0; i < r.ParamNames.Count; i++)
                {
                    // Decode URL-encoded values (for example %20 -> space)
                    routeParams[r.ParamNames[i]] = WebUtility.UrlDecode(m.Groups[i + 1].Value);
                }

                // Return the matching route handler
                return r.Handler;
            }

            // No route found --> return null
            return null;
        }
    }
}
