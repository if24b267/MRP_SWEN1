using MRP_SWEN1.Models;
using MRP_SWEN1.Repositories;
using MRP_SWEN1.Services;
using Npgsql;
using Dapper;
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
        private readonly string _connStr;

        public MediaController(IMediaRepository mediaRepo, IRatingRepository ratingRepo,
                       AuthService auth, string connStr)
        {
            _mediaRepo = mediaRepo;
            _ratingRepo = ratingRepo;
            _auth = auth;
            _connStr = connStr;
        }

        // POST /api/media
        // Create media
        // Requires Authorization header "Bearer <token>".
        // Body: MediaEntry JSON. We set CreatorUserId from the authenticated user.
        public async Task HandleCreate(RoutingRequest rr)
        {
            // Read Authorization header (may be missing)
            var authHeader = rr.Request.Headers["Authorization"] ?? "";

            // Check if the request is authenticated
            if (!_auth.TryAuthenticate(authHeader, out var info))
            {
                rr.Response.StatusCode = 401;
                await HttpServer.WriteResponse(rr.Response, new { error = "Unauthorized" });
                return;
            }

            // Read request body as raw JSON
            var body = await new StreamReader(rr.Request.InputStream).ReadToEndAsync();

            try
            {
                // Deserialize JSON body (case-insensitive to be more tolerant for clients)
                var m = JsonSerializer.Deserialize<MediaEntry>(
                    body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                // Payload could not be parsed into a MediaEntry
                if (m == null)
                {
                    rr.Response.StatusCode = 400;
                    await HttpServer.WriteResponse(rr.Response, new { error = "invalid payload" });
                    return;
                }

                // Set creator based on the authenticated user,
                // not on any client-provided value
                m.CreatorUserId = info.UserId;

                // Store media entry and return generated id
                var id = await _mediaRepo.Create(m);

                rr.Response.StatusCode = 201;
                await HttpServer.WriteResponse(rr.Response, new { id });
            }
            catch (Exception ex)
            {
                // Return a generic bad request error
                // Include the exception message to simplify debugging during development
                rr.Response.StatusCode = 400;
                await HttpServer.WriteResponse(
                    rr.Response,
                    new { error = "Bad request", detail = ex.Message }
                );
            }
        }

        // GET /api/media?title=...&genre=...&type=...&year=...&age=...&sort=...&order=...
        public async Task HandleSearch(RoutingRequest rr)
        {
            using var db = new NpgsqlConnection(_connStr);

            // Read query parameters from URL
            var title = rr.Request.QueryString["title"] ?? "";
            var genre = rr.Request.QueryString["genre"];
            var type = rr.Request.QueryString["type"];
            var yearStr = rr.Request.QueryString["year"];
            var ageStr = rr.Request.QueryString["age"];
            var sort = rr.Request.QueryString["sort"] ?? "title"; // default sort by title
            var order = rr.Request.QueryString["order"] ?? "asc";  // default order ascending

            // Validate sort and order values
            var validSort = new[] { "title", "year", "score" };
            var validOrder = new[] { "asc", "desc" };
            if (!validSort.Contains(sort) || !validOrder.Contains(order))
            {
                // If invalid, return 400 Bad Request
                rr.Response.StatusCode = 400;
                await HttpServer.WriteResponse(rr.Response, new { error = "invalid sort or order" });
                return;
            }

            // Convert year and age query parameters to nullable ints
            int? year = int.TryParse(yearStr, out var y) ? y : null;
            int? age = int.TryParse(ageStr, out var a) ? a : null;

            // Build the base SQL query dynamically
            // Filters are only applied if the parameter is provided
            var sql = @"SELECT 
                            m.id,
                            m.title,
                            m.description,
                            m.media_type,
                            m.release_year,
                            m.genres,
                            m.age_restriction,
                            m.creator_user_id,
                            (SELECT AVG(r.stars) 
                               FROM ratings r 
                               WHERE r.media_id = m.id AND r.confirmed = true) AS average_score
                        FROM media m
                        WHERE (@title = '' OR LOWER(m.title) LIKE LOWER(CONCAT('%', @title, '%')))
                          AND (@genre IS NULL OR @genre = ANY(m.genres))
                          AND (@type IS NULL OR m.media_type = @type)
                          AND (@year IS NULL OR m.release_year = @year)
                          AND (@age IS NULL OR m.age_restriction <= @age)
            ";

            // Determine which column to order by
            var orderBy = sort switch
            {
                "year" => "m.release_year",
                "score" => "average_score",
                _ => "m.title"
            };
            sql += $" ORDER BY {orderBy} {order.ToUpper()}"; // append sorting to SQL

            // Execute query
            var rows = await db.QueryAsync(sql, new { title, genre, type, year, age });

            // Map database rows to MediaEntry objects
            var list = rows.Select(row => new MediaEntry
            {
                Id = row.id,
                Title = row.title,
                Description = row.description,
                MediaType = row.media_type,
                ReleaseYear = row.release_year,
                Genres = ((string[])row.genres)?.ToList() ?? new List<string>(),
                AgeRestriction = row.age_restriction,
                CreatorUserId = row.creator_user_id
            });

            // Return results as JSON
            await HttpServer.WriteResponse(rr.Response, list);
        }


        // GET /api/media/{id}
        // Get media by id and include ratings for the media
        public async Task HandleGetById(RoutingRequest rr)
        {
            // Read id from route parameters and validate it
            var idStr = rr.RouteParams.ContainsKey("id") ? rr.RouteParams["id"] : null;
            if (!int.TryParse(idStr, out var id))
            {
                rr.Response.StatusCode = 400;
                await HttpServer.WriteResponse(rr.Response, new { error = "invalid id" });
                return;
            }

            // Load media entry from repository
            var m = await _mediaRepo.GetById(id);
            if (m == null)
            {
                rr.Response.StatusCode = 404;
                await HttpServer.WriteResponse(rr.Response, new { error = "not found" });
                return;
            }

            // Load all ratings for this media
            // Map them to a simple response object (DTO) for the client
            var ratings = (await _ratingRepo.GetByMediaId(id))
                .Select(r => new {
                    id = r.Id,
                    mediaId = r.MediaId,
                    userId = r.UserId,
                    stars = r.Stars,
                    comment = r.Comment,
                    timestamp = r.Timestamp,
                    confirmed = r.Confirmed
                })
                .ToList();

            var averageScore = ratings.Any() ? ratings.Average(r => r.stars) : (double?)null;

            // Return media together with its ratings
            await HttpServer.WriteResponse(rr.Response, new { media = m, ratings, averageScore });
        }

        // PUT /api/media/{id}
        // Update media entry
        // Only the original creator is allowed to update it
        public async Task HandleUpdate(RoutingRequest rr)
        {
            // Read and verify Authorization header
            var authHeader = rr.Request.Headers["Authorization"] ?? "";
            if (!_auth.TryAuthenticate(authHeader, out var info))
            {
                rr.Response.StatusCode = 401;
                await HttpServer.WriteResponse(rr.Response, new { error = "Unauthorized" });
                return;
            }

            // Read media id from route and validate it
            var idStr = rr.RouteParams.ContainsKey("id") ? rr.RouteParams["id"] : null;
            if (!int.TryParse(idStr, out var id))
            {
                rr.Response.StatusCode = 400;
                await HttpServer.WriteResponse(rr.Response, new { error = "invalid id" });
                return;
            }

            // Load existing media entry
            var existing = await _mediaRepo.GetById(id);
            if (existing == null)
            {
                rr.Response.StatusCode = 404;
                await HttpServer.WriteResponse(rr.Response, new { error = "not found" });
                return;
            }

            // Only the creator of the media is allowed to update it
            if (existing.CreatorUserId != info.UserId)
            {
                rr.Response.StatusCode = 403;
                await HttpServer.WriteResponse(rr.Response, new { error = "forbidden" });
                return;
            }

            // Read update payload from request body
            var body = await new StreamReader(rr.Request.InputStream).ReadToEndAsync();
            var upd = JsonSerializer.Deserialize<MediaEntry>(
                body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            if (upd == null)
            {
                rr.Response.StatusCode = 400;
                await HttpServer.WriteResponse(rr.Response, new { error = "invalid payload" });
                return;
            }

            // Preserve immutable fields (id and creator)
            // Client-provided values for these fields are ignored
            upd.Id = id;
            upd.CreatorUserId = existing.CreatorUserId;

            await _mediaRepo.Update(upd);
            await HttpServer.WriteResponse(rr.Response, new { message = "updated" });
        }

        // DELETE /api/media/{id}
        // Delete media entry
        // Only the creator is allowed to delete it
        public async Task HandleDelete(RoutingRequest rr)
        {
            // Read and verify Authorization header
            var authHeader = rr.Request.Headers["Authorization"] ?? "";
            if (!_auth.TryAuthenticate(authHeader, out var info))
            {
                rr.Response.StatusCode = 401;
                await HttpServer.WriteResponse(rr.Response, new { error = "Unauthorized" });
                return;
            }

            // Read media id from route and validate it
            var idStr = rr.RouteParams.ContainsKey("id") ? rr.RouteParams["id"] : null;
            if (!int.TryParse(idStr, out var id))
            {
                rr.Response.StatusCode = 400;
                await HttpServer.WriteResponse(rr.Response, new { error = "invalid id" });
                return;
            }

            // Load existing media entry
            var existing = await _mediaRepo.GetById(id);
            if (existing == null)
            {
                rr.Response.StatusCode = 404;
                await HttpServer.WriteResponse(rr.Response, new { error = "not found" });
                return;
            }

            // Only the creator of the media is allowed to delete it
            if (existing.CreatorUserId != info.UserId)
            {
                rr.Response.StatusCode = 403;
                await HttpServer.WriteResponse(rr.Response, new { error = "forbidden" });
                return;
            }

            // Delete dependent data (ratings and likes) before removing the media entry
            using var db = new NpgsqlConnection(_connStr);
            await db.ExecuteAsync(
                "DELETE FROM rating_likes WHERE rating_id IN (SELECT id FROM ratings WHERE media_id = @id);",
                new { id }
            );
            await db.ExecuteAsync(
                "DELETE FROM ratings WHERE media_id = @id;",
                new { id }
            );

            // Delete the media entry itself
            await _mediaRepo.Delete(id);
            await HttpServer.WriteResponse(rr.Response, new { message = "deleted" });
        }

        // -----------------
        // Ratings endpoints
        // -----------------

        // POST /api/media/{id}/rate
        // Creates a rating for the media by the current user.
        public async Task HandleRate(RoutingRequest rr)
        {
            // Read and verify Authorization header
            var authHeader = rr.Request.Headers["Authorization"] ?? "";
            if (!_auth.TryAuthenticate(authHeader, out var info))
            {
                rr.Response.StatusCode = 401;
                await HttpServer.WriteResponse(rr.Response, new { error = "Unauthorized" });
                return;
            }

            // Read media id from route and validate it
            var idStr = rr.RouteParams.ContainsKey("id") ? rr.RouteParams["id"] : null;
            if (!int.TryParse(idStr, out var mediaId))
            {
                rr.Response.StatusCode = 400;
                await HttpServer.WriteResponse(rr.Response, new { error = "invalid media id" });
                return;
            }

            // Ensure the media entry exists
            var media = await _mediaRepo.GetById(mediaId);
            if (media == null)
            {
                rr.Response.StatusCode = 404;
                await HttpServer.WriteResponse(rr.Response, new { error = "media not found" });
                return;
            }

            // Read request body (rating payload)
            var body = await new StreamReader(rr.Request.InputStream).ReadToEndAsync();
            try
            {
                // Parse JSON manually to validate required fields
                var doc = JsonSerializer.Deserialize<JsonElement>(body);

                // "stars" field is mandatory
                if (!doc.TryGetProperty("stars", out var s))
                {
                    rr.Response.StatusCode = 400;
                    await HttpServer.WriteResponse(rr.Response, new { error = "stars (1-5) required" });
                    return;
                }

                var stars = s.GetInt32();

                // Validate rating range (has to be 1..5)
                if (stars < 1 || stars > 5)
                {
                    rr.Response.StatusCode = 400;
                    await HttpServer.WriteResponse(rr.Response, new { error = "stars must be 1..5" });
                    return;
                }

                // Comment is optional
                var comment = doc.TryGetProperty("comment", out var c) ? c.GetString() : null;

                // Create rating linked to authenticated user and media
                var rating = new Rating
                {
                    MediaId = mediaId,
                    UserId = info.UserId,
                    Stars = stars,
                    Comment = comment,
                    Timestamp = DateTime.UtcNow,
                    Confirmed = false
                };

                // Store rating and return generated id
                var id = await _ratingRepo.Create(rating);
                rating.Id = id;

                rr.Response.StatusCode = 201;
                await HttpServer.WriteResponse(rr.Response, new { id });
            }
            catch (Exception ex)
            {
                // Invalid JSON payload
                rr.Response.StatusCode = 400;
                await HttpServer.WriteResponse(
                    rr.Response,
                    new { error = "invalid payload", detail = ex.Message }
                );
            }
        }

        // GET /api/media/{id}/ratings
        // Returns all ratings for a media (no auth required).
        public async Task HandleGetRatingsForMedia(RoutingRequest rr)
        {
            // Read media id from route and validate it
            var idStr = rr.RouteParams.ContainsKey("id") ? rr.RouteParams["id"] : null;
            if (!int.TryParse(idStr, out var mediaId))
            {
                rr.Response.StatusCode = 400;
                await HttpServer.WriteResponse(rr.Response, new { error = "invalid media id" });
                return;
            }

            // Ensure the media entry exists
            var media = await _mediaRepo.GetById(mediaId);
            if (media == null)
            {
                rr.Response.StatusCode = 404;
                await HttpServer.WriteResponse(rr.Response, new { error = "media not found" });
                return;
            }

            // Load and return all ratings for the given media
            var ratings = await _ratingRepo.GetByMediaId(mediaId);
            await HttpServer.WriteResponse(rr.Response, ratings);
        }

        // PUT /api/ratings/{id}/confirm
        public async Task HandleConfirmRatingComment(RoutingRequest rr)
        {
            var authHeader = rr.Request.Headers["Authorization"] ?? "";
            if (!_auth.TryAuthenticate(authHeader, out var info))
            {
                rr.Response.StatusCode = 401;
                await HttpServer.WriteResponse(rr.Response, new { error = "Unauthorized" });
                return;
            }

            var idStr = rr.RouteParams["id"];
            if (!int.TryParse(idStr, out var ratingId))
            {
                rr.Response.StatusCode = 400;
                await HttpServer.WriteResponse(rr.Response, new { error = "invalid rating id" });
                return;
            }

            var rating = await _ratingRepo.GetById(ratingId);
            if (rating == null)
            {
                rr.Response.StatusCode = 404;
                await HttpServer.WriteResponse(rr.Response, new { error = "rating not found" });
                return;
            }

            if (rating.UserId != info.UserId)
            {
                Console.WriteLine($"CONFIRM-FORBIDDEN: token-user={info.UserId}, rating-user={rating.UserId}, " +
                    $"rating-id={ratingId}");
                rr.Response.StatusCode = 403;
                await HttpServer.WriteResponse(rr.Response, 
                    new { error = $"forbidden: token for user {info.UserId}, rating belongs to {rating.UserId}" });
                return;
            }

            rating.Confirmed = true;
            await _ratingRepo.Update(rating);
            await HttpServer.WriteResponse(rr.Response, new { message = "confirmed" });
        }

        // PUT /api/ratings/{id}
        // Allows the owner of a rating to update it
        public async Task HandleUpdateRating(RoutingRequest rr)
        {
            // Read and verify Authorization header
            var authHeader = rr.Request.Headers["Authorization"] ?? "";
            if (!_auth.TryAuthenticate(authHeader, out var info))
            {
                rr.Response.StatusCode = 401;
                await HttpServer.WriteResponse(rr.Response, new { error = "Unauthorized" });
                return;
            }

            // Read rating id from route and validate it
            var idStr = rr.RouteParams.ContainsKey("id") ? rr.RouteParams["id"] : null;
            if (!int.TryParse(idStr, out var ratingId))
            {
                rr.Response.StatusCode = 400;
                await HttpServer.WriteResponse(rr.Response, new { error = "invalid rating id" });
                return;
            }

            // Load existing rating
            var existing = await _ratingRepo.GetById(ratingId);
            if (existing == null)
            {
                rr.Response.StatusCode = 404;
                await HttpServer.WriteResponse(rr.Response, new { error = "rating not found" });
                return;
            }

            // Only the user who created the rating may update it
            if (existing.UserId != info.UserId)
            {
                rr.Response.StatusCode = 403;
                await HttpServer.WriteResponse(rr.Response, new { error = "forbidden" });
                return;
            }

            // Read update payload from request body
            var body = await new StreamReader(rr.Request.InputStream).ReadToEndAsync();
            try
            {
                // Parse JSON manually to allow partial updates
                var doc = JsonSerializer.Deserialize<JsonElement>(body);

                // Update stars if provided
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

                // Update comment if provided
                if (doc.TryGetProperty("comment", out var c))
                {
                    existing.Comment = c.GetString();
                }

                // Editing a rating resets moderation state and updates timestamp
                existing.Confirmed = false;
                existing.Timestamp = DateTime.UtcNow;

                await _ratingRepo.Update(existing);
                await HttpServer.WriteResponse(rr.Response, new { message = "rating updated" });
            }
            catch (Exception ex)
            {
                // Invalid JSON payload
                rr.Response.StatusCode = 400;
                await HttpServer.WriteResponse(
                    rr.Response,
                    new { error = "invalid payload", detail = ex.Message }
                );
            }
        }

        // DELETE /api/ratings/{id}
        // Allows a user to delete their own rating
        public async Task HandleDeleteRating(RoutingRequest rr)
        {
            // Read and verify Authorization header
            var authHeader = rr.Request.Headers["Authorization"] ?? "";
            if (!_auth.TryAuthenticate(authHeader, out var info))
            {
                rr.Response.StatusCode = 401;
                await HttpServer.WriteResponse(rr.Response, new { error = "Unauthorized" });
                return;
            }

            // Read rating id from route and validate it
            var idStr = rr.RouteParams.ContainsKey("id") ? rr.RouteParams["id"] : null;
            if (!int.TryParse(idStr, out var ratingId))
            {
                rr.Response.StatusCode = 400;
                await HttpServer.WriteResponse(rr.Response, new { error = "invalid rating id" });
                return;
            }

            // Load existing rating
            var existing = await _ratingRepo.GetById(ratingId);
            if (existing == null)
            {
                rr.Response.StatusCode = 404;
                await HttpServer.WriteResponse(rr.Response, new { error = "rating not found" });
                return;
            }

            // Only the owner of the rating may delete it
            if (existing.UserId != info.UserId)
            {
                rr.Response.StatusCode = 403;
                await HttpServer.WriteResponse(rr.Response, new { error = "forbidden" });
                return;
            }

            // Delete dependent likes first
            using var db = new NpgsqlConnection(_connStr);
            await db.ExecuteAsync(
                "DELETE FROM rating_likes WHERE rating_id = @id;",
                new { id = ratingId }
            );

            // Then delete the rating itself
            await _ratingRepo.Delete(ratingId);

            await HttpServer.WriteResponse(rr.Response, new { message = "rating deleted" });
        }

        // GET /api/ratings/mine
        // Returns all ratings created by the authenticated user
        public async Task HandleGetMyRatings(RoutingRequest rr)
        {
            // Read and verify Authorization header
            var authHeader = rr.Request.Headers["Authorization"] ?? "";
            if (!_auth.TryAuthenticate(authHeader, out var info))
            {
                rr.Response.StatusCode = 401;
                return;
            }

            // Load and return all ratings for the current user
            var ratings = await _ratingRepo.GetByUserId(info.UserId);
            await HttpServer.WriteResponse(rr.Response, ratings);
        }
    }
}
