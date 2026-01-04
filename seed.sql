-- Demo-User (Passwort = leerer Hash, Salt)
INSERT INTO users (id, username, password_hash, salt, favorite_genre) VALUES
(1, 'admin', '\x00', '\x00', 'Sci-Fi'),
(2, 'alice', '\x00', '\x00', 'Comedy');

-- Demo-Media
INSERT INTO media (id, title, description, media_type, release_year, genres, age_restriction, creator_user_id) VALUES
(1, 'Interstellar', 'A team of explorers travel through a wormhole in space to ensure humanity''s survival.', 'movie', 2014, '{Sci-Fi,Drama}', 12, 1),
(2, 'The Office US', 'A mockumentary sitcom about office life.', 'series', 2005, '{Comedy}', 0, 2),
(3, 'Breaking Bad', 'A high-school chemistry teacher turns into a meth producer.', 'series', 2008, '{Crime,Drama}', 16, 1);

-- Demo-Ratings
INSERT INTO ratings (media_id, user_id, stars, comment, confirmed) VALUES
(1, 2, 5, 'Absolutely brilliant!', true),
(2, 1, 4, 'Hilarious, but too long.', true),
(3, 2, 5, 'Best show ever!', true);

-- Demo-Favorites
INSERT INTO favorites (user_id, media_id) VALUES
(1, 2),
(2, 1),
(2, 3);

-- Demo-Rating-Likes
INSERT INTO rating_likes (rating_id, user_id) VALUES
(1, 1),
(2, 2);