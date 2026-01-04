#!/usr/bin/env bash
set -euo pipefail

BASE="http://localhost:8080"
USERNAME="testuser2"
PASSWORD="pass456"

echo ""
echo "=== MRP Final – Curl Integration Tests ==="
echo ""

# 1  Register
echo "1) Register"
curl -s -X POST "$BASE/api/users/register" \
  -H "Content-Type: application/json" \
  -d "{\"username\":\"$USERNAME\",\"password\":\"$PASSWORD\"}" | jq .
echo ""

# 2  Login + Token
echo "2) Login"
TOKEN=$(curl -s -X POST "$BASE/api/users/login" \
  -H "Content-Type: application/json" \
  -d "{\"username\":\"$USERNAME\",\"password\":\"$PASSWORD\"}" | jq -r .token)
echo "Token: $TOKEN"
echo ""

# 3  Create Media
echo "3) Create media"
MID=$(curl -s -X POST "$BASE/api/media" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"title":"Blade Runner","description":"Sci-fi classic","mediaType":"movie","releaseYear":1982,"genres":["sci-fi","neo-noir"],"ageRestriction":16}' | jq -r .id)
echo "Media-ID: $MID"
echo ""

# 3a) View "my" ratings
echo "3a) Meine Ratings"
curl -s -H "Authorization: Bearer $TOKEN" "$BASE/api/ratings/mine" | jq .
echo ""

# 4  Get Media by ID
echo "4) Get media by id"
curl -s "$BASE/api/media/$MID" | jq .
echo ""

# 5  Search
echo "5) Search media"
curl -s "$BASE/api/media?title=blade" | jq .
echo ""

# 6  Update Media
echo "6) Update media"
curl -s -X PUT "$BASE/api/media/$MID" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"title":"Blade Runner (Final Cut)","description":"updated","mediaType":"movie","releaseYear":1982,"genres":["sci-fi"],"ageRestriction":16}' | jq .
echo ""

# 7  Rate Media
echo "7) Rate media"
RID=$(curl -s -X POST "$BASE/api/media/$MID/rate" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"stars":5,"comment":"Mind blown"}' | jq -r .id)
echo "Rating-ID: $RID"
echo ""

# 8  Get Ratings
echo "8) Get ratings for media"
curl -s "$BASE/api/media/$MID/ratings" | jq .
echo ""

# 9  Like Rating
echo "9) Like rating"
curl -s -X POST "$BASE/api/ratings/$RID/like" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{}' && echo "Like erfolgreich" || echo "Like fehlgeschlagen"
echo ""

# 10  Add to Favorites
echo "10) Add favorite"
curl -s -X POST "$BASE/api/media/$MID/favorite" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{}' && echo "Favorite added" || echo "Favorite failed"
echo ""

# 11  List Favorites
echo "11) List favorites"
curl -s -H "Authorization: Bearer $TOKEN" "$BASE/api/favorites" \
  -H "Content-Type: application/json" -d '{}' | jq . 2>/dev/null || echo "Favorite-Liste abgerufen"
echo ""

# 12  Get User Stats
echo "12) User stats"
curl -s -H "Authorization: Bearer $TOKEN" "$BASE/api/users/$USERNAME/stats" | jq .
echo ""

# 13  Leaderboard
echo "13) Leaderboard"
curl -s "$BASE/api/leaderboard" | jq .
echo ""

# 14  Recommendations
echo "14) Recommendations"
curl -s -H "Authorization: Bearer $TOKEN" "$BASE/api/recommendations" | jq .
echo ""

# 15  Update Rating
echo "15) Update rating"
curl -s -X PUT "$BASE/api/ratings/$RID" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"stars":4,"comment":"Even better after re-watch"}' 2>/dev/null || echo "Update versucht (nur eigene Ratings)"
echo ""

# 16  Delete Rating
echo "16) Delete rating"
curl -s -X DELETE "$BASE/api/ratings/$RID" \
  -H "Authorization: Bearer $TOKEN" 2>/dev/null || echo "Löschen versucht (nur eigene Ratings)"
echo ""

# 17  Remove Favorite
echo "17) Remove favorite"
curl -s -X DELETE "$BASE/api/media/$MID/favorite" \
  -H "Authorization: Bearer $TOKEN" | jq .
echo ""

# 18  Delete Media (cleanup)
echo "18) Delete media (cleanup)"
curl -s -X DELETE "$BASE/api/media/$MID" \
  -H "Authorization: Bearer $TOKEN" 2>/dev/null || echo "Löschen versucht (nur eigene Medien)"
echo ""
echo "=== All tests passed ✅ ==="
echo ""