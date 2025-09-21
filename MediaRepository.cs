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

        public Media GetMedia(int id) => MediaList.FirstOrDefault(m => m.MediaId == id);
        public List<Media> GetAllMedia() => MediaList;
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
            MediaList.Remove(media);
            media.Creator.CreatedMedia.Remove(media);
        }

        public List<Media> SearchByTitle(string query) => MediaList.Where(m => m.Title.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
    }
}
