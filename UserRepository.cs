namespace MRP_SWEN1
{
    public class UserRepository
    {
        private List<User> Users = new List<User>();
        private int nextId = 1;

        public User CreateUser(string username, string password)
        {
            var user = new User(nextId++, username, password);
            Users.Add(user);
            return user;
        }
        public User GetUser(string username) => Users.FirstOrDefault(user => user.Username == username);
        public List<User> GetAllUsers() => Users;

        public void RemoveUser(string username)
        {
            User user = GetUser(username);
            if (user != null)
            {
                Users.Remove(user);
            }
        }
    }
}
