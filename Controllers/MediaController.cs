using MRP_SWEN1.Auth;
using MRP_SWEN1.Models;
using MRP_SWEN1.Repositories;
using MRP_SWEN1.Services;
using System.Text.Json;

namespace MRP_SWEN1.Controllers
{
    // Controller responsible for media-related HTTP endpoints.
    // I kept controller methods thin: they parse request, call repositories/services,
    // and return JSON responses.
    public class MediaController
    {
        private readonly IMediaRepository _mediaRepo;
        private readonly IRatingRepository _ratingRepo;
        private readonly AuthService _auth;
        private readonly TokenStore _tokenStore;
         
        public MediaController(IMediaRepository mediaRepo, IRatingRepository ratingRepo, AuthService auth, TokenStore tokenStore)
        {
            _mediaRepo = mediaRepo;
            _ratingRepo = ratingRepo;
            _auth = auth;
            _tokenStore = tokenStore;
        }

        // Create media
        // Requires Authorization header "Bearer <token>".
        // Body: MediaEntry JSON. We set CreatorUserId from the authenticated user.
        public async Task HandleCreate(RoutingRequest rr)
        {
            var authHeader = rr.Request.Headers["Authorization"] ?? "";
            if (!_auth.TryAuthenticate(authHeader, out var info))
            {
                rr.Response.StatusCode = 401;
                await HttpServer.WriteResponse(rr.Response, new { error = "Unauthorized" });
                return;
            }

            var body = await new StreamReader(rr.Request.InputStream).ReadToEndAsync();
            try
            {
                // deserialize case-insensitively so clients do not have to match exact casing
                var m = JsonSerializer.Deserialize<MediaEntry>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (m == null)
                {
                    rr.Response.StatusCode = 400;
                    await HttpServer.WriteResponse(rr.Response, new { error = "invalid payload" });
                    return;
                }

                // ensure ownership is recorded
                m.CreatorUserId = info.UserId;
                var id = await _mediaRepo.Create(m);
                rr.Response.StatusCode = 201;
                await HttpServer.WriteResponse(rr.Response, new { id });
            }
            catch (Exception ex)
            {
                // return the message for easier debugging
                rr.Response.StatusCode = 400;
                await HttpServer.WriteResponse(rr.Response, new { error = "Bad request", detail = ex.Message });
            }
        }

        // Search media by title substring (query param: title)
        public async Task HandleSearch(RoutingRequest rr)
        {
            var query = rr.Request.QueryString["title"] ?? "";
            var list = await _mediaRepo.Search(query);
            await HttpServer.WriteResponse(rr.Response, list);
        }

        // Get media by id and include ratings for the media
        public async Task HandleGetById(RoutingRequest rr)
        {
            var idStr = rr.RouteParams.ContainsKey("id") ? rr.RouteParams["id"] : null;
            if (!int.TryParse(idStr, out var id))
            {
                rr.Response.StatusCode = 400;
                await HttpServer.WriteResponse(rr.Response, new { error = "invalid id" });
                return;
            }

            var m = await _mediaRepo.GetById(id);
            if (m == null)
            {
                rr.Response.StatusCode = 404;
                await HttpServer.WriteResponse(rr.Response, new { error = "not found" });
                return;
            }

            // include ratings for this media. Map to a DTO shape for response.
            var ratings = (await _ratingRepo.GetByMediaId(id)).Select(r => new {
                id = r.Id,
                mediaId = r.MediaId,
                userId = r.UserId,
                stars = r.Stars,
                comment = r.Comment,
                timestamp = r.Timestamp,
                confirmed = r.Confirmed
            }).ToList();

            await HttpServer.WriteResponse(rr.Response, new { media = m, ratings });
        }

        // Update media — only the creator may update
        // Body: MediaEntry JSON (we ignore CreatorUserId from client and keep original)
        public async Task HandleUpdate(RoutingRequest rr)
        {
            var authHeader = rr.Request.Headers["Authorization"] ?? "";
            if (!_auth.TryAuthenticate(authHeader, out var info))
            {
                rr.Response.StatusCode = 401;
                await HttpServer.WriteResponse(rr.Response, new { error = "Unauthorized" });
                return;
            }

            var idStr = rr.RouteParams.ContainsKey("id") ? rr.RouteParams["id"] : null;
            if (!int.TryParse(idStr, out var id))
            {
                rr.Response.StatusCode = 400;
                await HttpServer.WriteResponse(rr.Response, new { error = "invalid id" });
                return;
            }

            var existing = await _mediaRepo.GetById(id);
            if (existing == null)
            {
                rr.Response.StatusCode = 404;
                await HttpServer.WriteResponse(rr.Response, new { error = "not found" });
                return;
            }

            if (existing.CreatorUserId != info.UserId)
            {
                rr.Response.StatusCode = 403;
                await HttpServer.WriteResponse(rr.Response, new { error = "forbidden" });
                return;
            }

            var body = await new StreamReader(rr.Request.InputStream).ReadToEndAsync();
            var upd = JsonSerializer.Deserialize<MediaEntry>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (upd == null)
            {
                rr.Response.StatusCode = 400;
                await HttpServer.WriteResponse(rr.Response, new { error = "invalid payload" });
                return;
            }

            // preserve id and creator
            upd.Id = id;
            upd.CreatorUserId = existing.CreatorUserId;

            await _mediaRepo.Update(upd);
            await HttpServer.WriteResponse(rr.Response, new { message = "updated" });
        }

        // Delete media — only the creator may delete
        public async Task HandleDelete(RoutingRequest rr)
        {
            var authHeader = rr.Request.Headers["Authorization"] ?? "";
            if (!_auth.TryAuthenticate(authHeader, out var info))
            {
                rr.Response.StatusCode = 401;
                await HttpServer.WriteResponse(rr.Response, new { error = "Unauthorized" });
                return;
            }

            var idStr = rr.RouteParams.ContainsKey("id") ? rr.RouteParams["id"] : null;
            if (!int.TryParse(idStr, out var id))
            {
                rr.Response.StatusCode = 400;
                await HttpServer.WriteResponse(rr.Response, new { error = "invalid id" });
                return;
            }

            var existing = await _mediaRepo.GetById(id);
            if (existing == null)
            {
                rr.Response.StatusCode = 404;
                await HttpServer.WriteResponse(rr.Response, new { error = "not found" });
                return;
            }

            if (existing.CreatorUserId != info.UserId)
            {
                rr.Response.StatusCode = 403;
                await HttpServer.WriteResponse(rr.Response, new { error = "forbidden" });
                return;
            }

            await _mediaRepo.Delete(id);
            await HttpServer.WriteResponse(rr.Response, new { message = "deleted" });
        }

        // ----- Ratings endpoints -----

        // POST /api/media/{id}/rate
        // Creates a rating for the media by the current user.
        public async Task HandleRate(RoutingRequest rr)
        {
            var authHeader = rr.Request.Headers["Authorization"] ?? "";
            if (!_auth.TryAuthenticate(authHeader, out var info))
            {
                rr.Response.StatusCode = 401;
                await HttpServer.WriteResponse(rr.Response, new { error = "Unauthorized" });
                return;
            }

            var idStr = rr.RouteParams.ContainsKey("id") ? rr.RouteParams["id"] : null;
            if (!int.TryParse(idStr, out var mediaId))
            {
                rr.Response.StatusCode = 400;
                await HttpServer.WriteResponse(rr.Response, new { error = "invalid media id" });
                return;
            }

            var media = await _mediaRepo.GetById(mediaId);
            if (media == null)
            {
                rr.Response.StatusCode = 404;
                await HttpServer.WriteResponse(rr.Response, new { error = "media not found" });
                return;
            }

            var body = await new StreamReader(rr.Request.InputStream).ReadToEndAsync();
            try
            {
                var doc = JsonSerializer.Deserialize<JsonElement>(body);
                if (!doc.TryGetProperty("stars", out var s))
                {
                    rr.Response.StatusCode = 400;
                    await HttpServer.WriteResponse(rr.Response, new { error = "stars (1-5) required" });
                    return;
                }

                var stars = s.GetInt32();
                if (stars < 1 || stars > 5)
                {
                    rr.Response.StatusCode = 400;
                    await HttpServer.WriteResponse(rr.Response, new { error = "stars must be 1..5" });
                    return;
                }

                var comment = doc.TryGetProperty("comment", out var c) ? c.GetString() : null;

                var rating = new Rating
                {
                    MediaId = mediaId,
                    UserId = info.UserId,
                    Stars = stars,
                    Comment = comment,
                    Timestamp = DateTime.UtcNow,
                    Confirmed = false
                };

                var id = await _ratingRepo.Create(rating);
                rating.Id = id;

                rr.Response.StatusCode = 201;
                await HttpServer.WriteResponse(rr.Response, new { id });
            }
            catch (Exception ex)
            {
                rr.Response.StatusCode = 400;
                await HttpServer.WriteResponse(rr.Response, new { error = "invalid payload", detail = ex.Message });
            }
        }

        // GET /api/media/{id}/ratings
        // Returns all ratings for a media (no auth required).
        public async Task HandleGetRatingsForMedia(RoutingRequest rr)
        {
            var idStr = rr.RouteParams.ContainsKey("id") ? rr.RouteParams["id"] : null;
            if (!int.TryParse(idStr, out var mediaId))
            {
                rr.Response.StatusCode = 400;
                await HttpServer.WriteResponse(rr.Response, new { error = "invalid media id" });
                return;
            }

            var media = await _mediaRepo.GetById(mediaId);
            if (media == null)
            {
                rr.Response.StatusCode = 404;
                await HttpServer.WriteResponse(rr.Response, new { error = "media not found" });
                return;
            }

            var ratings = await _ratingRepo.GetByMediaId(mediaId);
            await HttpServer.WriteResponse(rr.Response, ratings);
        }

        // PUT /api/ratings/{id}  — edit rating by owner
        public async Task HandleUpdateRating(RoutingRequest rr)
        {
            var authHeader = rr.Request.Headers["Authorization"] ?? "";
            if (!_auth.TryAuthenticate(authHeader, out var info))
            {
                rr.Response.StatusCode = 401;
                await HttpServer.WriteResponse(rr.Response, new { error = "Unauthorized" });
                return;
            }

            var idStr = rr.RouteParams.ContainsKey("id") ? rr.RouteParams["id"] : null;
            if (!int.TryParse(idStr, out var ratingId))
            {
                rr.Response.StatusCode = 400;
                await HttpServer.WriteResponse(rr.Response, new { error = "invalid rating id" });
                return;
            }

            var existing = await _ratingRepo.GetById(ratingId);
            if (existing == null)
            {
                rr.Response.StatusCode = 404;
                await HttpServer.WriteResponse(rr.Response, new { error = "rating not found" });
                return;
            }

            if (existing.UserId != info.UserId)
            {
                rr.Response.StatusCode = 403;
                await HttpServer.WriteResponse(rr.Response, new { error = "forbidden" });
                return;
            }

            var body = await new StreamReader(rr.Request.InputStream).ReadToEndAsync();
            try
            {
                var doc = JsonSerializer.Deserialize<JsonElement>(body);
                if (doc.TryGetProperty("stars", out var s))
                {
                    var stars = s.GetInt32();
                    if (stars < 1 || stars > 5)
                    {
                        rr.Response.StatusCode = 400;
                        await HttpServer.WriteResponse(rr.Response, new { error = "stars must be 1..5" });
                        return;
                    }
                    existing.Stars = stars;
                }

                if (doc.TryGetProperty("comment", out var c))
                {
                    existing.Comment = c.GetString();
                }

                // editing resets moderation flag and updates timestamp
                existing.Confirmed = false;
                existing.Timestamp = DateTime.UtcNow;

                await _ratingRepo.Update(existing);
                await HttpServer.WriteResponse(rr.Response, new { message = "rating updated" });
            }
            catch (Exception ex)
            {
                rr.Response.StatusCode = 400;
                await HttpServer.WriteResponse(rr.Response, new { error = "invalid payload", detail = ex.Message });
            }
        }

        // DELETE /api/ratings/{id}
        // Only the rating owner may delete their rating.
        public async Task HandleDeleteRating(RoutingRequest rr)
        {
            var authHeader = rr.Request.Headers["Authorization"] ?? "";
            if (!_auth.TryAuthenticate(authHeader, out var info))
            {
                rr.Response.StatusCode = 401;
                await HttpServer.WriteResponse(rr.Response, new { error = "Unauthorized" });
                return;
            }

            var idStr = rr.RouteParams.ContainsKey("id") ? rr.RouteParams["id"] : null;
            if (!int.TryParse(idStr, out var ratingId))
            {
                rr.Response.StatusCode = 400;
                await HttpServer.WriteResponse(rr.Response, new { error = "invalid rating id" });
                return;
            }

            var existing = await _ratingRepo.GetById(ratingId);
            if (existing == null)
            {
                rr.Response.StatusCode = 404;
                await HttpServer.WriteResponse(rr.Response, new { error = "rating not found" });
                return;
            }

            if (existing.UserId != info.UserId)
            {
                rr.Response.StatusCode = 403;
                await HttpServer.WriteResponse(rr.Response, new { error = "forbidden" });
                return;
            }

            await _ratingRepo.Delete(ratingId);
            await HttpServer.WriteResponse(rr.Response, new { message = "rating deleted" });
        }
    }
}
