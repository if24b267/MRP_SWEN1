using System.Text.RegularExpressions;
using System.Net;

namespace MRP_SWEN1
{
    // Small routing utility that converts patterns like "/api/users/{username}/profile"
    // into a Regex and extracts named parameters.
    public class RoutingRequest
    {
        public HttpListenerRequest Request { get; set; }
        public HttpListenerResponse Response { get; set; }
        public Dictionary<string, string> RouteParams { get; set; } = new();
    }

    public delegate Task RouteHandler(RoutingRequest req);

    public class Router
    {
        private record RouteEntry(string Method, Regex Regex, List<string> ParamNames, RouteHandler Handler);

        private readonly List<RouteEntry> _routes = new();

        // Register a route pattern (method + path)
        public void Register(string method, string pathPattern, RouteHandler handler)
        {
            // convert /api/users/{username}/profile -> regex + capture names
            var paramNames = new List<string>();
            var pattern = "^" + Regex.Replace(pathPattern, @"\{([^}]+)\}", m => {
                paramNames.Add(m.Groups[1].Value);
                return "([^/]+)";
            }) + "$";
            var regex = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            _routes.Add(new RouteEntry(method.ToUpper(), regex, paramNames, handler));
        }

        // Match an incoming request and extract route parameters.
        public RouteHandler Match(string method, string path, out Dictionary<string, string> routeParams)
        {
            routeParams = new();
            foreach (var r in _routes)
            {
                if (!string.Equals(r.Method, method, StringComparison.OrdinalIgnoreCase)) continue;
                var m = r.Regex.Match(path);
                if (!m.Success) continue;
                for (int i = 0; i < r.ParamNames.Count; i++)
                {
                    routeParams[r.ParamNames[i]] = WebUtility.UrlDecode(m.Groups[i + 1].Value);
                }
                return r.Handler;
            }
            return null;
        }
    }
}
