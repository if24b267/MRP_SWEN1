# MRP_SWEN1 — Protokoll (Final Hand-In)

**Course:** SWEN1 — Media Rating Platform  
**Author:** Stefan Vukmirovic  
**Status:** Final hand-in  

---

## Kurze Zusammenfassung
Dieses Projekt erweitert den bestehenden REST-HTTP-Server aus dem *Intermediate Hand-In* zu einer vollständigen **Media Rating Platform (MRP)**.  
Die Anwendung basiert weiterhin auf **C# und HttpListener** (ohne ASP.NET), verwendet im Final jedoch **PostgreSQL** zur persistenten Datenspeicherung.

Für das *Final Hand-In* wurden die In-Memory-Repositories durch PostgreSQL-Implementierungen ersetzt, zusätzliche Business-Features umgesetzt sowie automatisierte Tests ergänzt.

---

## Ziel des Final Hand-Ins
Umsetzung aller geforderten **Final MUST-HAVEs**, insbesondere:
- funktionierender HTTP-Server mit Routing
- REST-Endpunkte (Auth, Media CRUD, Ratings, Favorites, Likes, Stats, Recommendations)
- Token-basierte Authentifizierung
- PostgreSQL-Anbindung (ohne ORM)
- Schutz vor SQL-Injection durch parametrisierte Queries
- Docker-Setup für die Datenbank
- mindestens **20 Unit-Tests**
- automatisierter Integrationstest (curl)

---

## Technische Entscheidungen
- **HttpListener:**  
  Reines HTTP ohne Frameworks (z. B. kein ASP.NET), um Routing- und Protokoll-Logik selbst umgesetzt.

- **PostgreSQL + Docker:**  
  Austausch der In-Memory-Repositories durch:
  - `PostgreSqlUserRepository`  
  - `PostgreSqlMediaRepository`  
  - `PostgreSqlRatingRepository`  
  --> Vorteil: persistente Speicherung und realistische Backend-Architektur.

- **Kein OR-Mapper:**  
  SQL-Zugriffe erfolgen ausschließlich über **manuell geschriebene, parametrisierte Queries** (`Npgsql`).

- **Architektur / Schichten:**
  - **Controller:** HTTP-bezogene Logik, Request/Response, Statuscodes
  - **Services:** Authentifizierung und Token-Verwaltung (`AuthService`, `TokenStore`)
  - **Repositories:** Datenzugriff über Interfaces (Dependency Injection)

- **Designprinzipien:**  
  Umsetzung von SOLID, insbesondere *Single Responsibility* und *Dependency Injection via Constructor*.

- **Routing:**  
  Eigene Router-Implementierung, die Pfad-Patterns wie `/api/media/{id}` in Regex umwandelt und Parameter extrahiert.

---

## Erweiterungen im Final
Zusätzlich zu den Basisfunktionen aus dem Intermediate wurden folgende Features umgesetzt:
- Favoritenverwaltung
- Rating-Likes
- einfache Statistiken (z. B. Leaderboard)
- Empfehlungen (Recommendations)

---

## Klassen / Komponenten (Kurzüberblick)
- `Program` — Einstiegspunkt; startet den Server im PostgreSQL-Modus  
- `HttpServer` — Verwaltung von `HttpListener`, Routen und Request-Verarbeitung  
- `Router` — Pfad-Matching und Parameter-Parsing  
- **Controller:**  
  - `UsersController`  
  - `MediaController`  
  - `FavoritesController`  
  - `StatisticsController`  
  - `RecommendationsController`  
  - `RatingLikesController`  
- **Services:** `AuthService`, `TokenStore`  
- **Repositories:** `IUserRepository`, `IMediaRepository`, `IRatingRepository` (+ PostgreSQL-Varianten)  
- **Modelle:** `User`, `MediaEntry`, `Rating`, `Favorite`, `RatingLike`

---

## Tests / Integration
- **Unit-Tests:**  
  Insgesamt mindestens **20 Unit-Tests** zur Überprüfung zentraler Geschäftslogik.

- **Integrationstest-Skript:** `mrp_curl_tests.sh` (bzw. `.ps1`)  
  Automatisierter Ablauf:
  - Register  
  - Login  
  - Create Media  
  - Rate  
  - Like  
  - Favorite  
  - Stats / Leaderboard  
  - Recommendations  
  - Cleanup  

- **Abhängigkeit:**  
  Das Skript nutzt `curl` und optional `jq` für JSON-Ausgabe.  
  Installation unter Windows (Git Bash):
  winget install jqlang.jq

  Danach kann das Skript mit
  chmod +x mrp_curl_tests.sh
  ./mrp_curl_tests.sh 
  ausgeführt werden.

- ---

## Probleme & Lösungen (Kurz)

- **Problem:**  
  `HttpListener` mit `http://+:8080/` benötigt unter Windows Administratorrechte.

- **Lösung:**  
  Verwendung von `http://localhost:8080/` als Fallback  
  **oder** Start des Servers im Administrator-Terminal.

---

## Hinweise zum Starten

Unter Windows benötigt `HttpListener` bei `http://+:8080/` Administratorrechte.

**Empfehlung für das Final Hand-In:**  
Server im Administrator-Terminal mit folgendem Befehl starten (Git Bash):
dotnet run

---

## Geschätzter Zeitaufwand

Der folgende Zeitaufwand bezieht sich auf die **Final-Erweiterung** des Projekts und ist **kumulativ** zum *Intermediate Hand-In* zu verstehen.

### Final-Erweiterung (kumulativ)

- PostgreSQL-Schema & Docker-Setup: **2 h**
- PostgreSQL-Repositories (3 Klassen): **4 h**
- Neue Controller (Favorites, Likes, Statistics, Recommendations): **3 h**
- Unit-Tests (mindestens 20 sinnvolle Tests): **2 h**
- Integrationstest (curl-Skript erweitern): **1 h**
- Protokoll aktualisieren: **1 h**

**Gesamt Final:** **13 h**

### Gesamtprojekt

- Intermediate Hand-In: **25 h**
- Final-Erweiterung: **13 h**

**Gesamtaufwand Projekt:** **38 h**

## Link to Git
[https://github.com/if24b267/MRP_SWEN1.git](https://github.com/if24b267/MRP_SWEN1.git)