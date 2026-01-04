using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using MRP_SWEN1.Models;
using Npgsql;

namespace MRP_SWEN1.Repositories
{
    public class PostgreSqlMediaRepository : IMediaRepository
    {
        private readonly string _connStr;
        public PostgreSqlMediaRepository(string connStr) => _connStr = connStr;

        public async Task<int> Create(MediaEntry media)
        {
            using var db = new NpgsqlConnection(_connStr);
            const string sql = @"INSERT INTO media (title, description, media_type, release_year, genres, age_restriction, creator_user_id)
                         VALUES (@Title, @Description, @MediaType, @ReleaseYear, @Genres, @AgeRestriction, @CreatorUserId)
                         RETURNING id;";
            return await db.QuerySingleAsync<int>(sql, new
            {
                media.Title,
                media.Description,
                media.MediaType,
                media.ReleaseYear,
                Genres = media.Genres?.ToArray() ?? Array.Empty<string>(),
                media.AgeRestriction,
                media.CreatorUserId
            });
        }

        public async Task<MediaEntry?> GetById(int id)
        {
            using var db = new NpgsqlConnection(_connStr);
            const string sql = "SELECT id, title, description, media_type, release_year, genres, age_restriction, creator_user_id FROM media WHERE id = @id;";
            var row = await db.QuerySingleOrDefaultAsync<dynamic>(sql, new { id });
            if (row == null) return null;

            return new MediaEntry
            {
                Id = row.id,
                Title = row.title,
                Description = row.description,
                MediaType = row.media_type,
                ReleaseYear = row.release_year,
                Genres = ((string[])row.genres)?.ToList() ?? new List<string>(),
                AgeRestriction = row.age_restriction,
                CreatorUserId = row.creator_user_id
            };
        }

        public async Task<IEnumerable<MediaEntry>> Search(string titleFilter)
        {
            using var db = new NpgsqlConnection(_connStr);
            const string sql = @"SELECT * FROM media
                         WHERE LOWER(title) LIKE LOWER(CONCAT('%', @titleFilter, '%'))
                         ORDER BY title;";
            var rows = await db.QueryAsync<dynamic>(sql, new { titleFilter });
            return rows.Select(row => new MediaEntry
            {
                Id = row.id,
                Title = row.title,
                Description = row.description,
                MediaType = row.media_type,
                ReleaseYear = row.release_year,
                Genres = ((string[])row.genres)?.ToList() ?? new List<string>(),
                AgeRestriction = row.age_restriction,
                CreatorUserId = row.creator_user_id
            });
        }

        public async Task Update(MediaEntry media)
        {
            using var db = new NpgsqlConnection(_connStr);
            const string sql = @"UPDATE media
                         SET title=@Title, description=@Description, media_type=@MediaType,
                             release_year=@ReleaseYear, genres=@Genres, age_restriction=@AgeRestriction
                         WHERE id = @Id;";
            var rows = await db.ExecuteAsync(sql, media);
            if (rows == 0)
                throw new KeyNotFoundException("media not found");
        }

        public async Task Delete(int id)
        {
            using var db = new NpgsqlConnection(_connStr);
            const string sql = "DELETE FROM media WHERE id = @id;";
            await db.ExecuteAsync(sql, new { id });
        }
    }
}