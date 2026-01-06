-- 1. USERS
--    - username UNIQUE  -->  registration must be unique (business rule)
--    - password_hash / salt stored as BYTEA  -->  raw bytes, no string conversion hassles
--    - favorite_genre NULLable  --> optional profile field, can be set later
CREATE TABLE users (
    id SERIAL PRIMARY KEY,
    username TEXT UNIQUE NOT NULL,
    password_hash BYTEA NOT NULL,
    salt BYTEA NOT NULL,
    favorite_genre TEXT
);

-- 2. MEDIA
--    - media_type CHECK  -->  only 'movie','series','game' allowed (spec requirement)
--    - genres TEXT[]  -->  PostgreSQL native array: easy UNNEST in recommendations
--    - creator_user_id FK  -->  ownership logic: only creator may edit / delete
--    - age_restriction INT  -->  simple number (0,6,12,16,18) instead of enum for flexibility
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

-- 3. RATINGS
--    - UNIQUE(media_id, user_id)  -->  business rule: one rating per user & media
--    - confirmed BOOLEAN DEFAULT FALSE  -->  moderation flag: comment invisible until owner confirms
--    - stars CHECK (1-5)  -->  spec range; DB enforces validity so controller does not need to trust input
--    - timestamp TIMESTAMPTZ  -->  timezone-aware, ordered listings (newest first) out of the box
CREATE TABLE ratings (
    id SERIAL PRIMARY KEY,
    media_id INT REFERENCES media(id) ON DELETE CASCADE,
    user_id INT REFERENCES users(id) ON DELETE CASCADE,
    stars INT CHECK (stars BETWEEN 1 AND 5),
    comment TEXT,
    timestamp TIMESTAMPTZ DEFAULT NOW(),
    confirmed BOOLEAN DEFAULT FALSE,
    UNIQUE(media_id, user_id)
);

-- 4. RATING_LIKES
--    - Composite PK (rating_id, user_id)  -->  one like per user per rating (natural constraint)
--    - ON DELETE CASCADE on both FKs  -->  removing a rating or a user cleans up likes automatically
CREATE TABLE rating_likes (
    rating_id INT REFERENCES ratings(id) ON DELETE CASCADE,
    user_id INT REFERENCES users(id) ON DELETE CASCADE,
    PRIMARY KEY (rating_id, user_id)
);

-- 5. FAVORITES
--    - Composite PK  -->  same idea: one favourite-star per user per media
--    - No extra surrogate id  -->  keeps table narrow, PK is the only index needed
--    - ON DELETE CASCADE  -->  delete user or media --> favourite vanishes (no orphaned rows)
CREATE TABLE favorites (
    user_id INT REFERENCES users(id) ON DELETE CASCADE,
    media_id INT REFERENCES media(id) ON DELETE CASCADE,
    PRIMARY KEY (user_id, media_id)
);