namespace MRP_SWEN1.Models
{
    // User model: store password hash + salt (byte[]). Public profile fields are optional.
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = "";
        public byte[] PasswordHash { get; set; } = Array.Empty<byte>();
        public byte[] Salt { get; set; } = Array.Empty<byte>();
        public string? FavoriteGenre { get; set; }
    }
}
