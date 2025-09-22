using MRP_SWEN1;

public class Rating
{
    private static int nextId = 1;
    public int RatingId { get; }
    public User Author { get; }
    public Media MediaEntry { get; }
    public int Stars { get; set; }

    private string comment;
    public string GetComment()
    {
        return IsCommentVisible ? comment : "[Comment hidden]";
    }

    private bool IsCommentVisible { get; set; } = false;
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

    public bool AddLike(User user) => likedBy.Add(user);
    public bool RemoveLike(User user) => likedBy.Remove(user);

    public void ConfirmComment() => IsCommentVisible = true;
    public bool IsVisible() => IsCommentVisible;

    public void EditComment(string newComment)
    {
        comment = newComment;
        IsCommentVisible = false;
    }
}

