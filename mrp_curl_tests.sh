#!/usr/bin/env bash
set -euo pipefail

BASE="http://localhost:8080"
USER="testuser_$(date +%s)"
PASS="demo1234"

echo "Starte frische Demo mit User: $USER"

# 1. Registrieren
echo "1) Registriere neuen User"
curl -s -X POST "$BASE/api/users/register" \
  -H "Content-Type: application/json" \
  -d "{\"username\":\"$USER\",\"password\":\"$PASS\"}" | jq .
echo

# 2. Einloggen & Token speichern
echo "2) Login & Token holen"
TOKEN=$(curl -s -X POST "$BASE/api/users/login" \
  -H "Content-Type: application/json" \
  -d "{\"username\":\"$USER\",\"password\":\"$PASS\"}" | jq -r .token)
echo "Token: $TOKEN"
echo

# 3. Media anlegen (1)
echo "3) Erstelle Media-Eintrag 1"
MID1=$(curl -s -X POST "$BASE/api/media" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"title":"Dune","description":"Epic sci-fi","mediaType":"movie","releaseYear":2021,"genres":["sci-fi"],"ageRestriction":12}' | jq -r .id)
echo "Media-ID 1: $MID1"
echo

# 4. Media anlegen (2)
echo "4) Erstelle Media-Eintrag 2"
MID2=$(curl -s -X POST "$BASE/api/media" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"title":"The Expanse","description":"Space opera series","mediaType":"series","releaseYear":2015,"genres":["sci-fi"],"ageRestriction":16}' | jq -r .id)
echo "Media-ID 2: $MID2"
echo

# 5. Media bewerten (1)
echo "5) Bewerte Media 1 (5 Sterne)"
RID1=$(curl -s -X POST "$BASE/api/media/$MID1/rate" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"stars":5,"comment":"Visually stunning"}' | jq -r .id)
echo "Rating-ID 1: $RID1"
echo

# 6. Rating bestätigen
echo "6) Bestätige Rating"
RESPONSE=$(curl -s -X PUT "$BASE/api/ratings/$RID1/confirm" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{}')
echo "Antwort: $RESPONSE"
echo

# 7. Media bewerten (2)
echo "7) Bewerte Media 2 (4 Sterne)"
RID2=$(curl -s -X POST "$BASE/api/media/$MID2/rate" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"stars":4,"comment":"Great world-building"}' | jq -r .id)
echo "Rating-ID 2: $RID2"
echo

# 8. Rating liken
echo "8) Like eigenes Rating"
LIKE_RESP=$(curl -s -X POST "$BASE/api/ratings/$RID2/like" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{}')
echo "Like-Antwort: $LIKE_RESP"
echo

# 9. Favorisieren
echo "9) Favorisiere Media 1"
FAV_RESP=$(curl -s -X POST "$BASE/api/media/$MID1/favorite" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{}')
echo "Favoriten-Antwort: $FAV_RESP"
echo

# 10. Favoriten anzeigen
echo "10) Zeige Favoriten"
curl -s -H "Authorization: Bearer $TOKEN" "$BASE/api/favorites" | jq .
echo

# 11. Empfehlungen
echo "11) Empfehlungen (sci-fi)"
curl -s -H "Authorization: Bearer $TOKEN" "$BASE/api/recommendations" | jq .
echo

# 12. Statistiken
echo "12) Persönliche Statistiken"
curl -s -H "Authorization: Bearer $TOKEN" "$BASE/api/users/$USER/stats" | jq .
echo

# 13. Leaderboard
echo "13) Leaderboard"
curl -s "$BASE/api/leaderboard" | jq .
echo

# 14. Rating updaten
echo "14) Update Rating (Sterne auf 3)"
curl -s -X PUT "$BASE/api/ratings/$RID2" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"stars":3,"comment":"Nach rewatch nur 3 Sterne"}' | jq .
echo

# 15. Rating löschen
echo "15) Lösche Rating 2"
curl -s -X DELETE "$BASE/api/ratings/$RID2" \
  -H "Authorization: Bearer $TOKEN" | jq .
echo

# 16. Favorit entfernen
echo "16) Entferne Favorit"
curl -s -X DELETE "$BASE/api/media/$MID1/favorite" \
  -H "Authorization: Bearer $TOKEN" | jq .
echo

# 17. Media löschen
echo "17) Lösche Media 1"
curl -s -X DELETE "$BASE/api/media/$MID1" \
  -H "Authorization: Bearer $TOKEN" | jq .
echo

echo "Demo abgeschlossen!"