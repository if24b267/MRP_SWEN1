namespace MRP_SWEN1
{
    public enum MediaType
    {
        Movie,
        Series,
        Game
    }

    public class Media
    {
        public int MediaId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public MediaType Type { get; set; }
        public int ReleaseYear { get; set; }
        public List<string> Genres { get; set; }
        public int AgeRestriction { get; set; }
        public User Creator { get; set; }
        public List<Rating> Ratings { get; set; } = new List<Rating>();

        public Media(int mediaId, string title, string description, MediaType type, int releaseYear, List<string> genres, int ageRestriction, User creator)
        {
            MediaId = mediaId;
            Title = title;
            Description = description;
            Type = type;
            ReleaseYear = releaseYear;
            Genres = genres;
            AgeRestriction = ageRestriction;
            Creator = creator;
        }

        public void AddGenre(string genre) => Genres.Add(genre);

        public double AverageRating() => Ratings.Count == 0 ? 0 : Ratings.Average(r => r.Stars);

        public IEnumerable<Rating> GetVisibleRatings() => Ratings.Where(rating => rating.IsCommentVisible);

        public void PrintInfo()
        {
            Console.WriteLine($"{Type} {Title} {ReleaseYear} - Age {AgeRestriction}+");
            Console.WriteLine($"Genres: {string.Join(", ", Genres)}");
            Console.WriteLine($"Average Rating: {AverageRating()}");
            Console.WriteLine($"Created by: {Creator.Username}\n");
        }
    }
}
