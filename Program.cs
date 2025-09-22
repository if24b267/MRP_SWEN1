namespace MRP_SWEN1
{
    internal class Program
    {
        static void Main(string[] args)
        {
            UserRepository userRepo = new UserRepository();
            MediaRepository mediaRepo = new MediaRepository();

            //User creation
            User alice = userRepo.CreateUser("alice", "123456");
            User bob = userRepo.CreateUser("bob", "xoHPV2zYeouVNcMk");

            //Media Creation:
            Media movie = mediaRepo.CreateMedia("Inception", "A mind-bending thriller", MediaType.Movie, 2010, new List<string> { "Sci-Fi", "Action" }, 12, alice);
            Media series = mediaRepo.CreateMedia("Stranger Things", "Mystery in Hawkins", MediaType.Series, 2016, new List<string> { "Mystery", "Horror" }, 14, bob);

            // neues Rating
            Rating rating = alice.RateMedia(movie, 5, "Toller Film!");
            Console.WriteLine($"Vor Edit: {rating.Stars} Sterne - {rating.GetComment()}\n");

            // Bob gibt Rating ab
            Rating ratingFromBob = bob.RateMedia(series, 4, "Gute Serie!");
            Console.WriteLine($"{ratingFromBob.Stars} Sterne - {ratingFromBob.GetComment()}\n");

            // Kommentar von Alice bestätigen
            rating.ConfirmComment();
            Console.WriteLine($"Nach Bestätigung: {rating.Stars} Sterne - {rating.GetComment()}\n");

            alice.EditRating(rating, 4, "Immer noch gut, aber nicht perfekt.");
            Console.WriteLine($"Nach Edit: {rating.Stars} Sterne - {rating.GetComment()}\n");

            // löschen
            alice.DeleteRating(rating, movie);
            Console.WriteLine($"Ratings von Alice: {alice.MyRatings.Count}, Media-Ratings: {movie.Ratings.Count}\n");

            //Favorites
            alice.AddToFavorites(series);

            //Demo outputs
            Console.WriteLine("Media List:\n");
            foreach (Media media in mediaRepo.GetAllMedia())
            {
                media.PrintInfo();
            }
            Console.WriteLine();

            Console.WriteLine("User Profiles:\n");
            foreach(User user in userRepo.GetAllUsers())
            {
                user.PrintProfile();
            }

            Console.WriteLine();

            //Search example
            Console.WriteLine("Search Media by 'Stranger': ");
            List<Media> searchResult = mediaRepo.SearchByTitle("Stranger");
            foreach(Media media in searchResult)
            {
                media.PrintInfo();
            }

            Console.ReadKey();
        }
    }
}
