namespace MRP_SWEN1
{
    public class Rating
    {
        public int RatingId { get; set; }
        public User Author { get; set; }
        public Media MediaEntry { get; set; }
        public int Stars { get; set; }
        public string Comment { get; set; }
        public bool IsCommentVisible { get; set; } = false;
        public DateTime CreatedAt { get; set; }

        private HashSet<User> likedBy = new HashSet<User>();
        public IReadOnlyCollection<User> LikedBy => likedBy;

        public Rating(User author,  Media mediaEntry, int stars, string comment = "")
        {
            RatingId = 0;
            Author = author;
            MediaEntry = mediaEntry;
            Stars = stars;
            Comment = comment;
            CreatedAt = DateTime.Now;
        }

        public bool AddLike(User user) => likedBy.Add(user);
        public bool RemoveLike(User user) => likedBy.Remove(user);
        public void ConfirmComment() => IsCommentVisible = true;
    }
}
