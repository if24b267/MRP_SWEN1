namespace MRP_SWEN1
{
    public class MediaRepository
    {
        private List<Media> MediaList = new List<Media>();
        private static int nextId = 1;

        public Media CreateMedia(string title, string description, MediaType type, int releaseYear, List<string> genres, int ageRestriction, User creator)
        {
            Media media = new Media(nextId++, title, description, type, releaseYear, genres, ageRestriction, creator);
            MediaList.Add(media);
            creator.CreatedMedia.Add(media);

            return media;
        }

        public Media GetMedia(int mediaId) => MediaList.FirstOrDefault(m => m.MediaId == mediaId);
        public List<Media> GetAllMedia()
        {
            return MediaList.Select(m => new Media(
                m.MediaId,
                m.Title,
                m.Description,
                m.Type,
                m.ReleaseYear,
                new List<string>(m.Genres),
                m.AgeRestriction,
                m.Creator
            )
            {
                Ratings = new List<Rating>(m.Ratings)
            }).ToList();
        }

        public void UpdateMedia(Media media, string title = null, string description = null)
        {
            if (title != null)
            {
                media.Title = title;
            }
            if (description != null)
            {
                media.Description = description;
            }
        }

        public void DeleteMedia(Media media)
        {
            if (media == null || !MediaList.Contains(media))
            {
                return;
            }

            foreach (Rating rating in media.Ratings)
            {
                rating.Author.MyRatings.Remove(rating);
                media.Ratings.Remove(rating);
            }

            media.Creator?.CreatedMedia.Remove(media);

            MediaList.Remove(media);
        }


        public List<Media> SearchByTitle(string query) => MediaList.Where(media => media.Title.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
    }
}
