using MRP_SWEN1;

public class Rating
{
    private static int nextId = 1;
    public int RatingId { get; }
    public User Author { get; }
    public Media MediaEntry { get; }
    public int Stars { get; set; }

    private string comment;
    private bool IsCommentVisible { get; set; } = false;
    public string GetComment() => IsCommentVisible ? comment : "[Comment hidden]";
    public DateTime CreatedAt { get; }

    private HashSet<User> likedBy = new HashSet<User>();
    public IReadOnlyCollection<User> LikedBy => likedBy;

    public Rating(User author, Media mediaEntry, int stars, string comment = "")
    {
        RatingId = nextId++;
        Author = author;
        MediaEntry = mediaEntry;
        Stars = stars;
        this.comment = comment;
        CreatedAt = DateTime.Now;
    }

    public bool Like(User user) => likedBy.Add(user);
    public bool Unlike(User user) => likedBy.Remove(user);

    public void ConfirmComment() => IsCommentVisible = true;
    public bool IsVisible() => IsCommentVisible;

    public void EditComment(string newComment)
    {
        comment = newComment;
        IsCommentVisible = false;
    }
}

