namespace MRP_SWEN1
{
    public class User
    {
        public int UserId { get; }
        public string Username { get; set; }
        public string Password { get; set; }
        public List<Media> CreatedMedia { get; set; } = new List<Media>();
        public List<Rating> MyRatings { get; set; } = new List<Rating>();
        public List<Media> Favorites { get; set; } = new List<Media>();

        public User(int userId, string username, string password)
        {
            UserId = userId;
            Username = username;
            Password = password;
        }

        public Rating RateMedia(Media mediaEntry, int stars, string comment = "")
        {
            if (stars < 1 || stars > 5)
            {
                throw new ArgumentOutOfRangeException(nameof(stars), "Stars must be between 1 and 5.");
            }

            Rating rating = new Rating(this, mediaEntry, stars, comment);
            MyRatings.Add(rating);
            mediaEntry.Ratings.Add(rating);

            return rating;
        }

        public void EditRating(Rating rating, int newStars, string newComment = "")
        {
            if (!MyRatings.Contains(rating))
            {
                throw new InvalidOperationException("User can only edit their own ratings.");
            }

            if (newStars < 1 || newStars > 5)
            {
                throw new ArgumentOutOfRangeException(nameof(newStars), "Stars must be between 1 and 5.");
            }

            rating.Stars = newStars;
            rating.EditComment(newComment);
        }

        public void DeleteRating(Rating rating, Media mediaEntry)
        {
            if (!MyRatings.Contains(rating))
            {
                throw new InvalidOperationException("User can only delete their own ratings.");
            }

            MyRatings.Remove(rating);
            mediaEntry.Ratings.Remove(rating);
        }

        public void AddToFavorites(Media media)
        {
            if (!Favorites.Contains(media))
            {
                Favorites.Add(media);
            }
        }

        public void PrintProfile()
        {
            Console.WriteLine($"User: {Username}, Id: {UserId}");
            Console.WriteLine($"Created Media: {CreatedMedia.Count}");
            Console.WriteLine($"Ratings given: {MyRatings.Count}");
            Console.WriteLine($"Favorites: {Favorites.Count}\n");
        }
    }
}
