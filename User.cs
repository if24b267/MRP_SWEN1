namespace MRP_SWEN1
{
    // User model: store password hash + salt (byte[]). Public profile fields are optional.
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = "";
        public byte[] PasswordHash { get; set; } = Array.Empty<byte>();
        public byte[] Salt { get; set; } = Array.Empty<byte>();

        // public profile fields
        public string? Email { get; set; }
        public string? FavoriteGenre { get; set; }
    }
}
