namespace MRP_SWEN1.Models
{
    // Rating model stored in the rating repository
    public class Rating
    {
        public int Id { get; set; }
        public int MediaId { get; set; }
        public int UserId { get; set; }
        public int Stars { get; set; } // 1..5
        public string? Comment { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public bool Confirmed { get; set; } = false; // moderation flag (default false)

        // Only filled if joined via GetByMediaId
        public string? Username { get; set; }
    }
}
