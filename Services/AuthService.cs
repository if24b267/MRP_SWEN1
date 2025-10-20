using MRP_SWEN1.Auth;
using MRP_SWEN1.Models;
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
            if (existing != null)
            {
                return (false, "Username already exists");
            }

            // generate 16 random bytes as salt
            var salt = RandomNumberGenerator.GetBytes(16);

            // compute the password hash using PBKDF2 (see helper below)
            var hash = PBKDF2Hash(password, salt);

            // create User object with hash + salt (in-memory repo stores it)
            var user = new User { Username = username, PasswordHash = hash, Salt = salt };

            // store user and get assigned id (we do not use id here further,
            // but repository increments it so future users are unique)
            var id = await _userRepo.Create(user);

            return (true, null);
        }

        // Login: verifies password, issues a simple token, stores token in TokenStore.
        // Token format is simple and not meant for production — it's just demo-friendly.
        public async Task<(bool ok, string? token, string? error, User? user)> Login(string username, string password)
        {
            var user = await _userRepo.GetByUsername(username);
            if (user == null)
            {
                return (false, null, "Invalid username or password", null);
            }

            // recompute hash with the saved salt
            var hash = PBKDF2Hash(password, user.Salt);

            // constant-time comparison (prevents a simple timing leak)
            if (!CryptographicOperations.FixedTimeEquals(hash, user.PasswordHash))
            {
                return (false, null, "Invalid username or password", null);
            }

            var token = $"{username}-mrpToken-{Guid.NewGuid()}";

            // store token info in memory so we can validate it later
            _tokenStore.Add(token, new TokenInfo { Token = token, UserId = user.Id, Username = username });

            return (true, token, null, user);
        }

        // Parse Authorization header "Bearer <token>" and lookup token in TokenStore.
        // If valid, 'info' is set to the TokenInfo for the token and method returns true.
        public bool TryAuthenticate(string bearerHeader, out TokenInfo? info)
        {
            info = null;

            if (string.IsNullOrWhiteSpace(bearerHeader)) 
            { 
                return false; 
            }

            var parts = bearerHeader.Split(' ', 2);
            if (parts.Length == 2 && parts[0].Equals("Bearer", StringComparison.OrdinalIgnoreCase))
            {
                var token = parts[1];
                if (_tokenStore.TryGet(token, out var tokenInfo))
                {
                    info = tokenInfo;
                    return true;
                }
            }

            return false;
        }

        // Helper: derive 32-byte key from password+salt using PBKDF2 with SHA256.
        // "Password-Based Key Derivation Function 2"
        private static byte[] PBKDF2Hash(string password, byte[] salt)
        {
            using var derive = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
            return derive.GetBytes(32);
        }
    }
}
