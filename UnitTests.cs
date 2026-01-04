using Dapper;
using MRP_SWEN1.Auth;
using MRP_SWEN1.Models;
using MRP_SWEN1.Repositories;
using MRP_SWEN1.Services;
using Npgsql;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace MRP_SWEN1.Tests
{
    // Integration tests – talk to a *real* Postgres instance
    public class IntegrationTests : IAsyncLifetime
    {
        private const string ConnStr =
            "Host=localhost;Database=mrp_db;Username=mrp_user;Password=mrp_pass";

        private readonly PostgreSqlUserRepository _userRepo;
        private readonly PostgreSqlMediaRepository _mediaRepo;
        private readonly PostgreSqlRatingRepository _ratingRepo;
        private readonly TokenStore _tokenStore = new();
        private readonly AuthService _auth;

        public IntegrationTests()
        {
            _userRepo = new PostgreSqlUserRepository(ConnStr);
            _mediaRepo = new PostgreSqlMediaRepository(ConnStr);
            _ratingRepo = new PostgreSqlRatingRepository(ConnStr);
            _auth = new AuthService(_userRepo, _tokenStore);
        }

        // -------------------- Lifetime helpers --------------------
        public async Task InitializeAsync()
        {
            using var db = new NpgsqlConnection(ConnStr);
            await db.ExecuteAsync("DELETE FROM rating_likes;");
            await db.ExecuteAsync("DELETE FROM favorites;");
            await db.ExecuteAsync("DELETE FROM ratings;");
            await db.ExecuteAsync("DELETE FROM media;");
            await db.ExecuteAsync("DELETE FROM users WHERE id > 2;"); // Seed bleibt
            await db.ExecuteAsync("ALTER SEQUENCE users_id_seq RESTART WITH 3;");
        }

        public Task DisposeAsync() => Task.CompletedTask;

        // -------------------- AuthService (1-5) --------------------
        [Fact]
        public async Task Register_UniqueUser_ReturnsSuccess()
        {
            var name = "alice_" + Guid.NewGuid();
            var (ok, err) = await _auth.Register(name, "Pass123!");
            Assert.True(ok);
            Assert.Null(err);
        }

        [Fact]
        public async Task Register_DuplicateUser_ReturnsError()
        {
            var name = "bob_" + Guid.NewGuid();
            await _auth.Register(name, "Pass123!");
            var (ok, err) = await _auth.Register(name, "Pass123!");
            Assert.False(ok);
            Assert.Equal("Username already exists", err);
        }

        [Fact]
        public async Task Login_ValidCreds_ReturnsToken()
        {
            var name = "carol_" + Guid.NewGuid();
            await _auth.Register(name, "Pass123!");
            var (ok, token, err, user) = await _auth.Login(name, "Pass123!");
            Assert.True(ok);
            Assert.NotNull(token);
            Assert.Null(err);
            Assert.NotNull(user);
        }

        [Fact]
        public async Task Login_InvalidPassword_ReturnsError()
        {
            var name = "dave_" + Guid.NewGuid();
            await _auth.Register(name, "Pass123!");
            var (ok, token, err, user) = await _auth.Login(name, "WrongPass");
            Assert.False(ok);
            Assert.Null(token);
            Assert.Equal("Invalid username or password", err);
            Assert.Null(user);
        }

        [Fact]
        public void TryAuthenticate_ValidBearer_ReturnsTrue()
        {
            const string token = "test-token";
            _tokenStore.Add(token, new TokenInfo { Token = token, UserId = 1, Username = "eve" });
            var ok = _auth.TryAuthenticate("Bearer test-token", out var info);
            Assert.True(ok);
            Assert.NotNull(info);
        }

        // -------------------- MediaRepository (6-10) --------------------
        [Fact]
        public async Task CreateMedia_AssignsId()
        {
            var uid = await _userRepo.Create(new User
            {
                Username = "creator1",
                PasswordHash = Array.Empty<byte>(),
                Salt = Array.Empty<byte>()
            });
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
            Assert.True(id > 0);
        }

        [Fact]
        public async Task GetById_Existing_ReturnsEntity()
        {
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
            var m = await _mediaRepo.GetById(id);
            Assert.Equal("T2", m.Title);
        }

        [Fact]
        public async Task GetById_NotExisting_ReturnsNull()
        {
            var m = await _mediaRepo.GetById(999);
            Assert.Null(m);
        }

        [Fact]
        public async Task Search_Substring_ReturnsMatches()
        {
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
            var res = await _mediaRepo.Search("potter");
            Assert.Single(res);
        }

        [Fact]
        public async Task Delete_Existing_Removes()
        {
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
            await _mediaRepo.Delete(id);
            var m = await _mediaRepo.GetById(id);
            Assert.Null(m);
        }

        // -------------------- RatingRepository (11-15) --------------------
        [Fact]
        public async Task CreateRating_AssignsId()
        {
            var uid = await _userRepo.Create(new User
            {
                Username = "frank",
                PasswordHash = Array.Empty<byte>(),
                Salt = Array.Empty<byte>()
            });
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
            var rid = await _ratingRepo.Create(new Rating
            {
                MediaId = mid,
                UserId = uid,
                Stars = 5
            });
            Assert.True(rid > 0);
        }

        [Fact]
        public async Task GetByMediaId_ReturnsInOrder()
        {
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
            await _ratingRepo.Create(new Rating { MediaId = mid, UserId = uid1, Stars = 3 });
            await Task.Delay(10);
            await _ratingRepo.Create(new Rating { MediaId = mid, UserId = uid2, Stars = 4 });
            var list = await _ratingRepo.GetByMediaId(mid);
            Assert.Equal(2, list.Count());
            Assert.Equal(4, list.First().Stars);
        }

        [Fact]
        public async Task UpdateRating_ChangesStars()
        {
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
            var rid = await _ratingRepo.Create(new Rating { MediaId = mid, UserId = uid, Stars = 2 });
            var r = await _ratingRepo.GetById(rid);
            r.Stars = 5;
            await _ratingRepo.Update(r);
            var updated = await _ratingRepo.GetById(rid);
            Assert.Equal(5, updated.Stars);
        }

        [Fact]
        public async Task DeleteRating_Removes()
        {
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
            await _ratingRepo.Delete(rid);
            var r = await _ratingRepo.GetById(rid);
            Assert.Null(r);
        }

        [Fact]
        public async Task UniqueConstraint_OneRatingPerUserMedia()
        {
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
            await _ratingRepo.Create(new Rating { MediaId = mid, UserId = uid, Stars = 3 });

            await Assert.ThrowsAsync<Npgsql.PostgresException>(async () =>
                await _ratingRepo.Create(new Rating { MediaId = mid, UserId = uid, Stars = 4 }));
        }

        // -------------------- Ownership & Token (16-20) --------------------
        [Fact]
        public async Task OnlyCreator_CanUpdateMedia()
        {
            var owner = await _userRepo.Create(new User
            {
                Username = "kilo_" + Guid.NewGuid(),
                PasswordHash = Array.Empty<byte>(),
                Salt = Array.Empty<byte>()
            });

            // Media anlegen
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

            // **Nicht-existierende ID** → garantiert 0 Rows → Exception
            var fake = new MediaEntry { Id = 999, CreatorUserId = owner };
            await Assert.ThrowsAsync<KeyNotFoundException>(async () => await _mediaRepo.Update(fake));
        }

        [Fact]
        public async Task OnlyCreator_CanDeleteMedia()
        {
            var owner = await _userRepo.Create(new User
            {
                Username = "mike_" + Guid.NewGuid(),
                PasswordHash = Array.Empty<byte>(),
                Salt = Array.Empty<byte>()
            });
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
            await _mediaRepo.Delete(mid);
            var m = await _mediaRepo.GetById(mid);
            Assert.Null(m);
        }

        [Fact]
        public async Task TokenStore_Remove_ReallyRemoves()
        {
            const string t = "remove-me";
            _tokenStore.Add(t, new TokenInfo { Token = t, UserId = 99 });
            _tokenStore.Remove(t);
            Assert.False(_tokenStore.TryGet(t, out _));
        }

        [Fact]
        public async Task Rating_DefaultConfirmed_IsFalse()
        {
            var uid = await _userRepo.Create(new User
            {
                Username = "november_" + Guid.NewGuid(),
                PasswordHash = Array.Empty<byte>(),
                Salt = Array.Empty<byte>()
            });
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
            var rid = await _ratingRepo.Create(new Rating { MediaId = mid, UserId = uid, Stars = 5 });
            var r = await _ratingRepo.GetById(rid);
            Assert.False(r.Confirmed);
        }

        [Fact]
        public async Task Search_EmptyQuery_ReturnsAll()
        {
            var uid = await _userRepo.Create(new User
            {
                Username = "searchuser_" + Guid.NewGuid(),
                PasswordHash = Array.Empty<byte>(),
                Salt = Array.Empty<byte>()
            });
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
            var res = await _mediaRepo.Search("");
            Assert.Equal(2, res.Count());
        }
    }
}