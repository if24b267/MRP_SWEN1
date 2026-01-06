-- Demo-User (password = empty hash, salt)
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

-- Additional users (same empty-password convention)
INSERT INTO users (id, username, password_hash, salt, favorite_genre) VALUES
(3, 'bob',     '\x00', '\x00', 'Action'),
(4, 'charlie', '\x00', '\x00', 'Horror'),
(5, 'dave',    '\x00', '\x00', 'Drama'),
(6, 'eve',     '\x00', '\x00', 'Comedy'),
(7, 'frank',   '\x00', '\x00', 'Sci-Fi'),
(8, 'grace',   '\x00', '\x00', 'Crime');

-- More media: mix of movies, series, games
INSERT INTO media (id, title, description, media_type, release_year, genres, age_restriction, creator_user_id) VALUES
(4, 'The Dark Knight', 'Batman faces the Joker in Gotham.', 'movie', 2008, '{Action,Crime}', 16, 3),
(5, 'Inception', 'A thief who steals corporate secrets through dream-sharing technology.', 'movie', 2010, '{Sci-Fi,Action}', 13, 7),
(6, 'The Witcher', 'Monster hunter Geralt struggles to find his place in a world where people often prove more wicked than beasts.', 'series', 2019, '{Fantasy,Action}', 16, 3),
(7, 'The Last of Us', 'A smuggler escorts a teenage girl across a post-apocalyptic United States.', 'series', 2023, '{Drama,Horror}', 18, 4),
(8, 'The Legend of Zelda: Breath of the Wild', 'Open-world adventure across the kingdom of Hyrule.', 'game', 2017, '{Adventure,Fantasy}', 12, 8),
(9, 'Red Dead Redemption 2', 'Tale of outlaw Arthur Morgan and the Van der Linde gang.', 'game', 2018, '{Action,Drama}', 18, 5),
(10, 'Stranger Things', 'Kids in a small town uncover supernatural mysteries.', 'series', 2016, '{Sci-Fi,Horror}', 16, 7),
(11, 'The Grand Budapest Hotel', 'Adventures of a legendary concierge at a famous European hotel.', 'movie', 2014, '{Comedy,Drama}', 12, 6),
(12, 'Parasite', 'A poor family schemes to become employed by a wealthy family.', 'movie', 2019, '{Thriller,Drama}', 16, 5);

-- Additional ratings (mix of stars & confirmed states)
INSERT INTO ratings (media_id, user_id, stars, comment, confirmed) VALUES
(4, 3, 5, 'Best Batman movie ever!', true),
(4, 4, 4, 'Great action, but dark.', true),
(5, 2, 5, 'Mind-blowing concept!', true),
(5, 3, 3, 'Too confusing for me.', false),
(6, 1, 4, 'Good adaptation of the books.', true),
(6, 8, 5, 'Geralt is perfect!', true),
(7, 4, 5, 'Emotional and gripping.', true),
(7, 6, 4, 'Zombies done right.', true),
(8, 8, 5, 'Best Zelda ever!', true),
(8, 2, 4, 'Beautiful but challenging.', true),
(9, 5, 5, 'Masterpiece of storytelling.', true),
(9, 3, 4, 'Slow start, amazing ending.', false),
(10, 7, 5, 'Nostalgia overload!', true),
(10, 2, 3, 'Gets repetitive.', false),
(11, 6, 4, 'Wesley at his best.', true),
(12, 5, 5, 'Deserved every Oscar.', true),
(12, 4, 4, 'Unpredictable twists.', true);

-- More favorites
INSERT INTO favorites (user_id, media_id) VALUES
(3, 4), (3, 6), (4, 7), (5, 9), (5, 12), (6, 11), (7, 5), (7, 10), (8, 6), (8, 8);

-- More rating-likes
INSERT INTO rating_likes (rating_id, user_id) VALUES
(3, 1), (4, 2), (5, 3), (6, 4), (7, 5), (8, 6), (9, 7), (10, 8), (11, 1), (12, 2);