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
    public class UnitTests
    {
        private readonly IUserRepository _userRepo = new InMemoryUserRepository();
        private readonly IMediaRepository _mediaRepo = new InMemoryMediaRepository();
        private readonly IRatingRepository _ratingRepo = new InMemoryRatingRepository();
        private readonly TokenStore _tokenStore = new();
        private readonly AuthService _auth;

        public UnitTests() => _auth = new AuthService(_userRepo, _tokenStore);

        /* ---------- 1-5: AuthService ---------- */
        [Fact]
        public async Task Register_UniqueUser_ReturnsSuccess()
        {
            var (ok, err) = await _auth.Register("alice", "Pass123!");
            Assert.True(ok); Assert.Null(err);
        }

        [Fact]
        public async Task Register_DuplicateUser_ReturnsError()
        {
            await _auth.Register("bob", "Pass123!");
            var (ok, err) = await _auth.Register("bob", "Pass123!");
            Assert.False(ok); Assert.Equal("Username already exists", err);
        }

        [Fact]
        public async Task Login_ValidCreds_ReturnsToken()
        {
            await _auth.Register("carol", "Pass123!");
            var (ok, token, err, user) = await _auth.Login("carol", "Pass123!");
            Assert.True(ok); Assert.NotNull(token); Assert.Null(err); Assert.NotNull(user);
        }

        [Fact]
        public async Task Login_InvalidPassword_ReturnsError()
        {
            await _auth.Register("dave", "Pass123!");
            var (ok, token, err, user) = await _auth.Login("dave", "WrongPass");
            Assert.False(ok); Assert.Null(token); Assert.Equal("Invalid username or password", err); Assert.Null(user);
        }

        [Fact]
        public void TryAuthenticate_ValidBearer_ReturnsTrue()
        {
            const string token = "test-token";
            _tokenStore.Add(token, new TokenInfo { Token = token, UserId = 1, Username = "eve" });
            var ok = _auth.TryAuthenticate("Bearer test-token", out var info);
            Assert.True(ok); Assert.NotNull(info);
        }

        /* ---------- 6-10: MediaRepository ---------- */
        [Fact]
        public async Task CreateMedia_AssignsId()
        {
            var id = await _mediaRepo.Create(new MediaEntry { Title = "T1" });
            Assert.True(id > 0);
        }

        [Fact]
        public async Task GetById_Existing_ReturnsEntity()
        {
            var id = await _mediaRepo.Create(new MediaEntry { Title = "T2" });
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
            await _mediaRepo.Create(new MediaEntry { Title = "Harry Potter" });
            await _mediaRepo.Create(new MediaEntry { Title = "Lord of the Rings" });
            var res = await _mediaRepo.Search("potter");
            Assert.Single(res);
        }

        [Fact]
        public async Task Delete_Existing_Removes()
        {
            var id = await _mediaRepo.Create(new MediaEntry { Title = "T3" });
            await _mediaRepo.Delete(id);
            var m = await _mediaRepo.GetById(id);
            Assert.Null(m);
        }

        /* ---------- 11-15: RatingRepository ---------- */
        [Fact]
        public async Task CreateRating_AssignsId()
        {
            var uid = await _userRepo.Create(new User { Username = "frank" });
            var mid = await _mediaRepo.Create(new MediaEntry { Title = "T4" });
            var rid = await _ratingRepo.Create(new Rating { MediaId = mid, UserId = uid, Stars = 5 });
            Assert.True(rid > 0);
        }

        [Fact]
        public async Task GetByMediaId_ReturnsInOrder()
        {
            var uid = await _userRepo.Create(new User { Username = "grace" });
            var mid = await _mediaRepo.Create(new MediaEntry { Title = "T5" });
            await _ratingRepo.Create(new Rating { MediaId = mid, UserId = uid, Stars = 3 });
            await _ratingRepo.Create(new Rating { MediaId = mid, UserId = uid, Stars = 4 });
            var list = await _ratingRepo.GetByMediaId(mid);
            Assert.Equal(2, list.Count());
        }

        [Fact]
        public async Task UpdateRating_ChangesStars()
        {
            var uid = await _userRepo.Create(new User { Username = "heidi" });
            var mid = await _mediaRepo.Create(new MediaEntry { Title = "T6" });
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
            var uid = await _userRepo.Create(new User { Username = "ivan" });
            var mid = await _mediaRepo.Create(new MediaEntry { Title = "T7" });
            var rid = await _ratingRepo.Create(new Rating { MediaId = mid, UserId = uid, Stars = 1 });
            await _ratingRepo.Delete(rid);
            var r = await _ratingRepo.GetById(rid);
            Assert.Null(r);
        }

        [Fact]
        public async Task UniqueConstraint_OneRatingPerUserMedia()
        {
            var uid = await _userRepo.Create(new User { Username = "juliet" });
            var mid = await _mediaRepo.Create(new MediaEntry { Title = "T8" });
            await _ratingRepo.Create(new Rating { MediaId = mid, UserId = uid, Stars = 3 });
            await Assert.ThrowsAsync<PostgresException>(async () =>
                await _ratingRepo.Create(new Rating { MediaId = mid, UserId = uid, Stars = 4 }));
        }

        /* ---------- 16-20: Ownership & Token ---------- */
        [Fact]
        public async Task OnlyCreator_CanUpdateMedia()
        {
            var owner = await _userRepo.Create(new User { Username = "kilo" });
            var intruder = await _userRepo.Create(new User { Username = "lima" });
            var mid = await _mediaRepo.Create(new MediaEntry { Title = "T9", CreatorUserId = owner });

            var m = await _mediaRepo.GetById(mid);
            m.CreatorUserId = intruder; // simuliert falsche Absicht
            await Assert.ThrowsAsync<KeyNotFoundException>(async () => await _mediaRepo.Update(m));
        }

        [Fact]
        public async Task OnlyCreator_CanDeleteMedia()
        {
            var owner = await _userRepo.Create(new User { Username = "mike" });
            var mid = await _mediaRepo.Create(new MediaEntry { Title = "T10", CreatorUserId = owner });
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
            var uid = await _userRepo.Create(new User { Username = "november" });
            var mid = await _mediaRepo.Create(new MediaEntry { Title = "T11" });
            var rid = await _ratingRepo.Create(new Rating { MediaId = mid, UserId = uid, Stars = 5 });
            var r = await _ratingRepo.GetById(rid);
            Assert.False(r.Confirmed);
        }

        [Fact]
        public async Task Search_EmptyQuery_ReturnsAll()
        {
            await _mediaRepo.Create(new MediaEntry { Title = "Alpha" });
            await _mediaRepo.Create(new MediaEntry { Title = "Beta" });
            var res = await _mediaRepo.Search("");
            Assert.Equal(2, res.Count());
        }
    }
}