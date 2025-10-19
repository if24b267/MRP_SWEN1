using MRP_SWEN1.Auth;
using MRP_SWEN1.Repositories;
using System.Security.Cryptography;

namespace MRP_SWEN1.Services
{
    // AuthService handles register/login + token creation.
    // Passwords are hashed with PBKDF2 and stored as byte[].
    // Salt + hash in memory (User model).
    public class AuthService
    {
        private readonly IUserRepository _userRepo;
        private readonly TokenStore _tokenStore;

        public AuthService(IUserRepository userRepo, TokenStore tokenStore)
        {
            _userRepo = userRepo;
            _tokenStore = tokenStore;
        }

        // Register: checks username uniqueness, creates salt+hash, stores user.
        public async Task<(bool ok, string? error)> Register(string username, string password)
        {
            var existing = await _userRepo.GetByUsername(username);
            if (existing != null) return (false, "Username already exists");

            var salt = RandomNumberGenerator.GetBytes(16);
            var hash = PBKDF2Hash(password, salt);
            var user = new User { Username = username, PasswordHash = hash, Salt = salt };
            var id = await _userRepo.Create(user);
            return (true, null);
        }

        // Login: verifies password, issues a simple token, stores token in TokenStore.
        // Token format is simple and not meant for production — it's just demo-friendly.
        public async Task<(bool ok, string? token, string? error, User? user)> Login(string username, string password)
        {
            var user = await _userRepo.GetByUsername(username);
            if (user == null) return (false, null, "Invalid username or password", null);
            var hash = PBKDF2Hash(password, user.Salt);
            if (!CryptographicOperations.FixedTimeEquals(hash, user.PasswordHash))
                return (false, null, "Invalid username or password", null);

            var token = $"{username}-mrpToken-{Guid.NewGuid()}";
            _tokenStore.Add(token, new TokenInfo { Token = token, UserId = user.Id, Username = username });
            return (true, token, null, user);
        }

        // Parse Authorization header "Bearer <token>" and lookup token in TokenStore.
        public bool TryAuthenticate(string bearerHeader, out TokenInfo? info)
        {
            info = null;
            if (string.IsNullOrWhiteSpace(bearerHeader)) return false;
            var parts = bearerHeader.Split(' ', 2);
            if (parts.Length == 2 && parts[0].Equals("Bearer", StringComparison.OrdinalIgnoreCase))
            {
                var token = parts[1];
                if (_tokenStore.TryGet(token, out var t))
                {
                    info = t;
                    return true;
                }
            }
            return false;
        }

        // Helper: derive 32-byte key from password+salt using PBKDF2 with SHA256.
        private static byte[] PBKDF2Hash(string password, byte[] salt)
        {
            using var derive = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
            return derive.GetBytes(32);
        }
    }
}
