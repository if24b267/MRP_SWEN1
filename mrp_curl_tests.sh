#!/usr/bin/env bash
#-----------------------------
# Testskript für unsere REST-API
# Führt Registrierung, Login, Media-CRUD und Rating-Tests durch
# -----------------------------
set -euo pipefail   # Stoppt Skript bei Fehlern, unbehandelten Variablen oder Pipeline-Fehlern

# Basis-URL des lokalen Servers
BASE="http://localhost:8080"

# Testbenutzer
USERNAME="testuser"
PASSWORD="pass123"


# 1) Benutzer registrieren
echo "1) Register"
curl -s -X POST "$BASE/api/users/register" \
  -H "Content-Type: application/json" \
  -d "{\"username\":\"$USERNAME\",\"password\":\"$PASSWORD\"}" \
  | jq .   # jq macht die JSON-Ausgabe schöner

echo

# 2) Login, um ein Bearer-Token für geschützte Endpoints zu erhalten
echo "2) Login"
LOGIN_RESP=$(curl -s -X POST "$BASE/api/users/login" \
  -H "Content-Type: application/json" \
  -d "{\"username\":\"$USERNAME\",\"password\":\"$PASSWORD\"}")
echo "$LOGIN_RESP" | jq .

# Token extrahieren
TOKEN=$(echo "$LOGIN_RESP" | jq -r .token)

# Prüfen, ob Token erhalten wurde
if [ -z "$TOKEN" ] || [ "$TOKEN" = "null" ]; then
  echo "Login failed or token missing. Response: $LOGIN_RESP"
  exit 1
fi
echo "Token: $TOKEN"

echo

# 3) Medien-Eintrag erstellen
echo "3) Create media"
CREATE_RESP=$(curl -s -X POST "$BASE/api/media" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"title":"Inception","description":"Sci-fi thriller","mediaType":"movie","releaseYear":2010,"genres":["sci-fi","thriller"],"ageRestriction":12}')
echo "$CREATE_RESP" | jq .

# ID des erstellten Media-Eintrags extrahieren
MEDIA_ID=$(echo "$CREATE_RESP" | jq -r .id)
if [ -z "$MEDIA_ID" ] || [ "$MEDIA_ID" = "null" ]; then
  echo "Create media failed. Exiting."
  exit 1
fi
echo "MEDIA_ID = $MEDIA_ID"

echo

# 4) Medien-Eintrag anhand der ID abrufen
echo "4) Get media by id"
curl -s "$BASE/api/media/$MEDIA_ID" | jq .

echo

# 5) Medien anhand Titel suchen
echo "5) Search media"
curl -s "$BASE/api/media?title=inception" | jq .

echo

# 6) Medien-Eintrag aktualisieren
echo "6) Update media"
curl -s -X PUT "$BASE/api/media/$MEDIA_ID" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"title":"Inception (demo)","description":"edited","mediaType":"movie","releaseYear":2010,"genres":["sci-fi"],"ageRestriction":12}' \
  | jq .

echo

# 7) Medien-Eintrag bewerten
echo "7) Rate media"
RATE_RESP=$(curl -s -X POST "$BASE/api/media/$MEDIA_ID/rate" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"stars":5,"comment":"Awesome!"}')
echo "$RATE_RESP" | jq .

# Rating-ID extrahieren für spätere Tests
RATING_ID=$(echo "$RATE_RESP" | jq -r .id)

echo

# 8) Alle Bewertungen für das Medium abrufen
echo "8) Get ratings for media"
curl -s "$BASE/api/media/$MEDIA_ID/ratings" | jq .

# 9) Bewertung aktualisieren (nur wenn Rating existiert)
if [ "$RATING_ID" != "null" ] && [ -n "$RATING_ID" ]; then
  echo
  echo "9) Update rating"
  curl -s -X PUT "$BASE/api/ratings/$RATING_ID" \
    -H "Authorization: Bearer $TOKEN" \
    -H "Content-Type: application/json" \
    -d '{"stars":4,"comment":"Actually, 4 stars"}' | jq .

  # 10) Bewertung löschen
  echo
  echo "10) Delete rating"
  curl -s -X DELETE "$BASE/api/ratings/$RATING_ID" \
    -H "Authorization: Bearer $TOKEN" | jq .
fi

echo

# 11) Benutzerprofil abrufen
echo "11) Get user profile"
curl -s -H "Authorization: Bearer $TOKEN" "$BASE/api/users/$USERNAME/profile" | jq .

echo

# 12) Medien-Eintrag löschen (Cleanup)
echo "12) Delete media (cleanup)"
curl -s -X DELETE "$BASE/api/media/$MEDIA_ID" -H "Authorization: Bearer $TOKEN" | jq .

echo
echo "== DONE =="
