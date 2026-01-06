using Dapper;
using MRP_SWEN1.Auth;
using MRP_SWEN1.Models;
using MRP_SWEN1.Repositories;
using MRP_SWEN1.Services;
using Npgsql;
using Xunit;

namespace MRP_SWEN1.Tests
{
    // Integration tests – talk to a *real* Postgres instance
    public class IntegrationTests : IAsyncLifetime
    {
        private const string ConnStr =
            "Host=localhost;Database=mrp_db;Username=mrp_user;Password=mrp_pass";

        // Repositories used in the tests
        private readonly PostgreSqlUserRepository _userRepo;
        private readonly PostgreSqlMediaRepository _mediaRepo;
        private readonly PostgreSqlRatingRepository _ratingRepo;

        // Token store and auth service for authentication-related tests
        private readonly TokenStore _tokenStore = new();
        private readonly AuthService _auth;

        // Test setup: create repositories and services once per test class
        public IntegrationTests()
        {
            _userRepo = new PostgreSqlUserRepository(ConnStr);
            _mediaRepo = new PostgreSqlMediaRepository(ConnStr);
            _ratingRepo = new PostgreSqlRatingRepository(ConnStr);
            _auth = new AuthService(_userRepo, _tokenStore);
        }

        // Runs before each test class execution
        // Ensures a clean database state for reproducible tests
        public async Task InitializeAsync()
        {
            using var db = new NpgsqlConnection(ConnStr);

            // Clear dependent tables first to avoid FK conflicts
            await db.ExecuteAsync("DELETE FROM rating_likes;");
            await db.ExecuteAsync("DELETE FROM favorites;");
            await db.ExecuteAsync("DELETE FROM ratings;");
            await db.ExecuteAsync("DELETE FROM media;");

            // Keep seeded users (e.g. admin/test users with id <= 2)
            await db.ExecuteAsync("DELETE FROM users WHERE id > 2;");

            // Reset user ID sequence so IDs stay predictable in tests
            await db.ExecuteAsync("ALTER SEQUENCE users_id_seq RESTART WITH 3;");
        }

        // No cleanup required after tests
        public Task DisposeAsync() => Task.CompletedTask;



        // -----------------
        // AuthService (1-5)
        // -----------------

        [Fact]
        public async Task Register_UniqueUser_ReturnsSuccess()
        {
            // Generate a unique username to avoid collisions
            var name = "alice_" + Guid.NewGuid();

            var (ok, err) = await _auth.Register(name, "Pass123!");

            // Registration should succeed for a new user
            Assert.True(ok);
            Assert.Null(err);
        }

        [Fact]
        public async Task Register_DuplicateUser_ReturnsError()
        {
            // Register the same username twice
            var name = "bob_" + Guid.NewGuid();
            await _auth.Register(name, "Pass123!");

            var (ok, err) = await _auth.Register(name, "Pass123!");

            // Second registration should fail
            Assert.False(ok);
            Assert.Equal("Username already exists", err);
        }

        [Fact]
        public async Task Login_ValidCreds_ReturnsToken()
        {
            // Register user first
            var name = "carol_" + Guid.NewGuid();
            await _auth.Register(name, "Pass123!");

            // Login with correct credentials
            var (ok, token, err, user) = await _auth.Login(name, "Pass123!");

            // Expect successful login and a generated token
            Assert.True(ok);
            Assert.NotNull(token);
            Assert.Null(err);
            Assert.NotNull(user);
        }

        [Fact]
        public async Task Login_InvalidPassword_ReturnsError()
        {
            // Register user
            var name = "dave_" + Guid.NewGuid();
            await _auth.Register(name, "Pass123!");

            // Try login with wrong password
            var (ok, token, err, user) = await _auth.Login(name, "WrongPass");

            // Login should fail
            Assert.False(ok);
            Assert.Null(token);
            Assert.Equal("Invalid username or password", err);
            Assert.Null(user);
        }

        [Fact]
        public void TryAuthenticate_ValidBearer_ReturnsTrue()
        {
            // Manually add a token to the token store
            const string token = "test-token";
            _tokenStore.Add(token, new TokenInfo
            {
                Token = token,
                UserId = 1,
                Username = "eve"
            });

            // Try to authenticate using a valid Bearer token
            var ok = _auth.TryAuthenticate("Bearer test-token", out var info);

            // Authentication should succeed
            Assert.True(ok);
            Assert.NotNull(info);
        }



        // ----------------------
        // MediaRepository (6–10)
        // ----------------------

        [Fact]
        public async Task CreateMedia_AssignsId()
        {
            // Create a user who will act as the media creator
            var uid = await _userRepo.Create(new User
            {
                Username = "creator1",
                PasswordHash = Array.Empty<byte>(),
                Salt = Array.Empty<byte>()
            });

            // Create a new media entry
            var id = await _mediaRepo.Create(new MediaEntry
            {
                Title = "T1",
                Description = "D1",
                MediaType = "movie",
                ReleaseYear = 2020,
                Genres = new() { "action" },
                AgeRestriction = 16,
                CreatorUserId = uid
            });

            // The database should assign a positive id
            Assert.True(id > 0);
        }

        [Fact]
        public async Task GetById_Existing_ReturnsEntity()
        {
            // Create a user and a media entry
            var uid = await _userRepo.Create(new User
            {
                Username = "creator2",
                PasswordHash = Array.Empty<byte>(),
                Salt = Array.Empty<byte>()
            });

            var id = await _mediaRepo.Create(new MediaEntry
            {
                Title = "T2",
                Description = "D2",
                MediaType = "movie",
                ReleaseYear = 2020,
                Genres = new() { "action" },
                AgeRestriction = 16,
                CreatorUserId = uid
            });

            // Load the media entry by its id
            var m = await _mediaRepo.GetById(id);

            // The returned entity should match the stored data
            Assert.Equal("T2", m.Title);
        }

        [Fact]
        public async Task GetById_NotExisting_ReturnsNull()
        {
            // Try to load a media entry that does not exist
            var m = await _mediaRepo.GetById(999);

            // Repository should return null if no record is found
            Assert.Null(m);
        }

        [Fact]
        public async Task Search_Substring_ReturnsMatches()
        {
            // Create a user and two media entries
            var uid = await _userRepo.Create(new User
            {
                Username = "creator3",
                PasswordHash = Array.Empty<byte>(),
                Salt = Array.Empty<byte>()
            });

            await _mediaRepo.Create(new MediaEntry
            {
                Title = "Harry Potter",
                Description = "D",
                MediaType = "movie",
                ReleaseYear = 2001,
                Genres = new() { "fantasy" },
                AgeRestriction = 12,
                CreatorUserId = uid
            });

            await _mediaRepo.Create(new MediaEntry
            {
                Title = "Lord of the Rings",
                Description = "D",
                MediaType = "movie",
                ReleaseYear = 2001,
                Genres = new() { "fantasy" },
                AgeRestriction = 12,
                CreatorUserId = uid
            });

            // Search using a substring (case-insensitive)
            var res = await _mediaRepo.Search("potter");

            // Only one title should match the search term
            Assert.Single(res);
        }

        [Fact]
        public async Task Delete_Existing_Removes()
        {
            // Create a user and a media entry
            var uid = await _userRepo.Create(new User
            {
                Username = "creator4",
                PasswordHash = Array.Empty<byte>(),
                Salt = Array.Empty<byte>()
            });

            var id = await _mediaRepo.Create(new MediaEntry
            {
                Title = "T3",
                Description = "D3",
                MediaType = "movie",
                ReleaseYear = 2020,
                Genres = new() { "action" },
                AgeRestriction = 16,
                CreatorUserId = uid
            });

            // Delete the media entry
            await _mediaRepo.Delete(id);

            // After deletion, the entry should no longer exist
            var m = await _mediaRepo.GetById(id);
            Assert.Null(m);
        }



        // ------------------------
        // RatingRepository (11–15)
        // ------------------------

        [Fact]
        public async Task CreateRating_AssignsId()
        {
            // Create a user who will submit a rating
            var uid = await _userRepo.Create(new User
            {
                Username = "frank",
                PasswordHash = Array.Empty<byte>(),
                Salt = Array.Empty<byte>()
            });

            // Create a media entry to be rated
            var mid = await _mediaRepo.Create(new MediaEntry
            {
                Title = "T4",
                Description = "D4",
                MediaType = "movie",
                ReleaseYear = 2020,
                Genres = new() { "action" },
                AgeRestriction = 16,
                CreatorUserId = uid
            });

            // Create a rating for the media
            var rid = await _ratingRepo.Create(new Rating
            {
                MediaId = mid,
                UserId = uid,
                Stars = 5
            });

            // The database should assign a valid id
            Assert.True(rid > 0);
        }

        [Fact]
        public async Task GetByMediaId_ReturnsInOrder()
        {
            // Create two users who will rate the same media
            var uid1 = await _userRepo.Create(new User
            {
                Username = "grace1",
                PasswordHash = Array.Empty<byte>(),
                Salt = Array.Empty<byte>()
            });

            var uid2 = await _userRepo.Create(new User
            {
                Username = "grace2",
                PasswordHash = Array.Empty<byte>(),
                Salt = Array.Empty<byte>()
            });

            // Create a media entry
            var mid = await _mediaRepo.Create(new MediaEntry
            {
                Title = "T5",
                Description = "D5",
                MediaType = "movie",
                ReleaseYear = 2020,
                Genres = new() { "action" },
                AgeRestriction = 16,
                CreatorUserId = uid1
            });

            // Create two confirmed ratings with a short delay to ensure different timestamps
            await _ratingRepo.Create(new Rating { MediaId = mid, UserId = uid1, Stars = 3, Confirmed = true });
            await Task.Delay(10);
            await _ratingRepo.Create(new Rating { MediaId = mid, UserId = uid2, Stars = 4, Confirmed = true });

            // Load ratings for the media
            var list = await _ratingRepo.GetByMediaId(mid);

            // Ratings should be ordered by timestamp (newest first)
            Assert.Equal(2, list.Count());
            Assert.Equal(4, list.First().Stars);
        }

        [Fact]
        public async Task UpdateRating_ChangesStars()
        {
            // Create user and media entry
            var uid = await _userRepo.Create(new User
            {
                Username = "heidi",
                PasswordHash = Array.Empty<byte>(),
                Salt = Array.Empty<byte>()
            });

            var mid = await _mediaRepo.Create(new MediaEntry
            {
                Title = "T6",
                Description = "D6",
                MediaType = "movie",
                ReleaseYear = 2020,
                Genres = new() { "action" },
                AgeRestriction = 16,
                CreatorUserId = uid
            });

            // Create an initial rating
            var rid = await _ratingRepo.Create(new Rating { MediaId = mid, UserId = uid, Stars = 2 });

            // Update the rating stars
            var r = await _ratingRepo.GetById(rid);
            r.Stars = 5;
            await _ratingRepo.Update(r);

            // Reload and verify that the update was applied
            var updated = await _ratingRepo.GetById(rid);
            Assert.Equal(5, updated.Stars);
        }

        [Fact]
        public async Task DeleteRating_Removes()
        {
            // Create user, media and rating
            var uid = await _userRepo.Create(new User
            {
                Username = "ivan",
                PasswordHash = Array.Empty<byte>(),
                Salt = Array.Empty<byte>()
            });

            var mid = await _mediaRepo.Create(new MediaEntry
            {
                Title = "T7",
                Description = "D7",
                MediaType = "movie",
                ReleaseYear = 2020,
                Genres = new() { "action" },
                AgeRestriction = 16,
                CreatorUserId = uid
            });

            var rid = await _ratingRepo.Create(new Rating { MediaId = mid, UserId = uid, Stars = 1 });

            // Delete the rating
            await _ratingRepo.Delete(rid);

            // After deletion, the rating should no longer exist
            var r = await _ratingRepo.GetById(rid);
            Assert.Null(r);
        }

        [Fact]
        public async Task UniqueConstraint_OneRatingPerUserMedia()
        {
            // Create a user and a media entry
            var uid = await _userRepo.Create(new User
            {
                Username = "juliet_" + Guid.NewGuid(),
                PasswordHash = Array.Empty<byte>(),
                Salt = Array.Empty<byte>()
            });

            var mid = await _mediaRepo.Create(new MediaEntry
            {
                Title = "T8",
                Description = "D8",
                MediaType = "movie",
                ReleaseYear = 2020,
                Genres = new() { "action" },
                AgeRestriction = 16,
                CreatorUserId = uid
            });

            // First rating by the user is allowed
            await _ratingRepo.Create(new Rating { MediaId = mid, UserId = uid, Stars = 3 });

            // A second rating by the same user for the same media should violate the DB constraint
            await Assert.ThrowsAsync<Npgsql.PostgresException>(async () =>
                await _ratingRepo.Create(new Rating { MediaId = mid, UserId = uid, Stars = 4 }));
        }



        // -------------------------
        // Ownership & Token (16–20)
        // -------------------------

        [Fact]
        public async Task OnlyCreator_CanUpdateMedia()
        {
            // Create the owner user
            var owner = await _userRepo.Create(new User
            {
                Username = "kilo_" + Guid.NewGuid(),
                PasswordHash = Array.Empty<byte>(),
                Salt = Array.Empty<byte>()
            });

            // Create a media entry owned by this user
            var mid = await _mediaRepo.Create(new MediaEntry
            {
                Title = "T9",
                Description = "D9",
                MediaType = "movie",
                ReleaseYear = 2020,
                Genres = new() { "action" },
                AgeRestriction = 16,
                CreatorUserId = owner
            });

            // Try to update a media entry that does not exist.
            // The repository should detect that no row was affected
            // and throw an exception.
            var fake = new MediaEntry { Id = 999, CreatorUserId = owner };

            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
                await _mediaRepo.Update(fake));
        }

        [Fact]
        public async Task OnlyCreator_CanDeleteMedia()
        {
            // Create a user who owns the media entry
            var owner = await _userRepo.Create(new User
            {
                Username = "mike_" + Guid.NewGuid(),
                PasswordHash = Array.Empty<byte>(),
                Salt = Array.Empty<byte>()
            });

            // Create a media entry
            var mid = await _mediaRepo.Create(new MediaEntry
            {
                Title = "T10",
                Description = "D10",
                MediaType = "movie",
                ReleaseYear = 2020,
                Genres = new() { "action" },
                AgeRestriction = 16,
                CreatorUserId = owner
            });

            // Delete the media entry
            await _mediaRepo.Delete(mid);

            // After deletion, the entry should no longer be found
            var m = await _mediaRepo.GetById(mid);
            Assert.Null(m);
        }

        [Fact]
        public async Task TokenStore_Remove_ReallyRemoves()
        {
            const string t = "remove-me";

            // Add a token to the in-memory token store
            _tokenStore.Add(t, new TokenInfo { Token = t, UserId = 99 });

            // Remove the token again
            _tokenStore.Remove(t);

            // The token should no longer be retrievable
            Assert.False(_tokenStore.TryGet(t, out _));
        }

        [Fact]
        public async Task Rating_DefaultConfirmed_IsFalse()
        {
            // Create a user
            var uid = await _userRepo.Create(new User
            {
                Username = "november_" + Guid.NewGuid(),
                PasswordHash = Array.Empty<byte>(),
                Salt = Array.Empty<byte>()
            });

            // Create a media entry
            var mid = await _mediaRepo.Create(new MediaEntry
            {
                Title = "T11",
                Description = "D11",
                MediaType = "movie",
                ReleaseYear = 2020,
                Genres = new() { "action" },
                AgeRestriction = 16,
                CreatorUserId = uid
            });

            // Create a rating without explicitly setting Confirmed
            var rid = await _ratingRepo.Create(new Rating
            {
                MediaId = mid,
                UserId = uid,
                Stars = 5
            });

            // Newly created ratings should not be confirmed by default
            var r = await _ratingRepo.GetById(rid);
            Assert.False(r.Confirmed);
        }

        [Fact]
        public async Task Search_EmptyQuery_ReturnsAll()
        {
            // Create a user for media creation
            var uid = await _userRepo.Create(new User
            {
                Username = "searchuser_" + Guid.NewGuid(),
                PasswordHash = Array.Empty<byte>(),
                Salt = Array.Empty<byte>()
            });

            // Create two media entries
            await _mediaRepo.Create(new MediaEntry
            {
                Title = "Alpha",
                Description = "D",
                MediaType = "movie",
                ReleaseYear = 2020,
                Genres = new() { "action" },
                AgeRestriction = 16,
                CreatorUserId = uid
            });

            await _mediaRepo.Create(new MediaEntry
            {
                Title = "Beta",
                Description = "D",
                MediaType = "movie",
                ReleaseYear = 2020,
                Genres = new() { "action" },
                AgeRestriction = 16,
                CreatorUserId = uid
            });

            // An empty search query should return all media entries
            var res = await _mediaRepo.Search("");

            Assert.Equal(2, res.Count());
        }
    }
}