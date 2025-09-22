namespace MRP_SWEN1
{
    public class UserRepository
    {
        private List<User> Users = new List<User>();
        private static int nextId = 1;

        public User CreateUser(string username, string password)
        {
            User user = new User(nextId++, username, password);
            Users.Add(user);
            return user;
        }

        public User GetUser(int userId) => Users.FirstOrDefault(user => user.UserId == userId);
        public List<User> GetAllUsers()
        {
            return Users.Select(u => new User(u.UserId, u.Username, u.Password)
            {
                CreatedMedia = new List<Media>(u.CreatedMedia),
                MyRatings = new List<Rating>(u.MyRatings),
                Favorites = new List<Media>(u.Favorites)
            }).ToList();
        }

        public void RemoveUser(int userId, MediaRepository mediaRepo)
        {
            User user = GetUser(userId);
            if (user != null)
            {
                foreach (Media media in user.CreatedMedia.ToList())
                {
                    mediaRepo.DeleteMedia(media);
                }

                foreach (Rating rating in user.MyRatings.ToList())
                {
                    rating.MediaEntry.Ratings.Remove(rating);
                }

                Users.Remove(user);
            }
        }

    }
}
