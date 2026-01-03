CREATE TABLE users (
    id SERIAL PRIMARY KEY,
    username TEXT UNIQUE NOT NULL,
    password_hash BYTEA NOT NULL,
    salt BYTEA NOT NULL,
    favorite_genre TEXT
);

CREATE TABLE media (
    id SERIAL PRIMARY KEY,
    title TEXT NOT NULL,
    description TEXT,
    media_type TEXT CHECK (media_type IN ('movie','series','game')),
    release_year INT,
    genres TEXT[],
    age_restriction INT,
    creator_user_id INT REFERENCES users(id)
);

CREATE TABLE ratings (
    id SERIAL PRIMARY KEY,
    media_id INT REFERENCES media(id),
    user_id INT REFERENCES users(id),
    stars INT CHECK (stars BETWEEN 1 AND 5),
    comment TEXT,
    timestamp TIMESTAMPTZ DEFAULT NOW(),
    confirmed BOOLEAN DEFAULT FALSE,
    UNIQUE(media_id, user_id)
);

CREATE TABLE rating_likes (
    rating_id INT REFERENCES ratings(id),
    user_id INT REFERENCES users(id),
    PRIMARY KEY (rating_id, user_id)
);

CREATE TABLE favorites (
    user_id INT REFERENCES users(id),
    media_id INT REFERENCES media(id),
    PRIMARY KEY (user_id, media_id)
);