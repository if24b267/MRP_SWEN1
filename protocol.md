# MRP_SWEN1 — Protokoll (Intermediate Hand-In)

**Course:** SWEN1 — Media Rating Platform  
**Author:** Stefan Vukmirovic  
**Status:** Intermediate hand-in  

---

## Kurze Zusammenfassung
Dieses Projekt implementiert einen kleinen REST-HTTP-Server in **C#** (basierend auf `HttpListener`) für eine **Media Ratings Platform (MRP)**.  
Für das *Intermediate Hand-In* läuft die Anwendung vollständig **im In-Memory-Modus**, das heißt ohne externe Datenbank.

Ziel war es, die geforderten **MUST-HAVEs** umzusetzen:
- funktionierender Server mit Routing,
- REST-Endpunkte (Register/Login/Media CRUD/Rating),
- Token-basierte Authentifikation,
- Modellklassen (`User`, `MediaEntry`, `Rating`),
- Integrationstest (automatisiert über Bash-Skript).

---

## Technische Entscheidungen
- **HttpListener:** Reines HTTP ohne Frameworks (z. B. kein ASP.NET).
- **In-Memory-Repositories:**  
  - `InMemoryUserRepository`  
  - `InMemoryMediaRepository`  
  - `InMemoryRatingRepository`  
  → Vorteil: kein Setup externer Datenbank notwendig.
- **Architektur / Schichten:**
  - **Controller:** HTTP-bezogene Logik, Request/Response.
  - **Services:** Logik für Authentifizierung und Token-Verwaltung (`AuthService`, `TokenStore`).
  - **Repositories:** Datenzugriff über Interfaces.
- **Designprinzipien:** Umsetzung von SOLID, v. a. *Single Responsibility* und *Dependency Injection via Constructor*.
- **Routing:** `Router` wandelt Pfad-Patterns wie `/api/media/{id}` in Regex um und extrahiert Parameter für Controller.

---

## Klassen / Komponenten (Kurzüberblick)
- `Program` — Einstiegspunkt; startet den Server im In-Memory-Modus.  
- `HttpServer` — Verwaltet `HttpListener`, Route-Registrierung, Request-Verarbeitung.  
- `Router` — Pfad-Matching und Parameter-Parsing.  
- `UsersController`, `MediaController` — Implementieren die REST-Endpunkte.  
- `AuthService`, `TokenStore` — Authentifizierung und Token-Verwaltung.  
- Repositories: `IUserRepository`, `IMediaRepository`, `IRatingRepository` (+ In-Memory-Versionen).  
- Modelle: `User`, `MediaEntry`, `Rating`.  

---

## Tests / Integration
- **Integrationstest-Skript:** `mrp_curl_tests.sh`  
  Führt automatisiert die gesamte Prozesskette aus:  
  → Register → Login → Create Media → Rate → Update/Delete Rating → Profile → Cleanup.  

- **Abhängigkeit:** Das Skript nutzt `curl` und `jq` (für JSON-Parsing).  
  - `jq` ist optional, aber empfehlenswert, da es die Ausgabe lesbarer macht.  
  - Installation unter Windows (Git Bash):  
    ```bash
    winget install jqlang.jq
    ```
  - Danach kann das Skript mit  
    ```bash
    chmod +x mrp_curl_tests.sh
    ./mrp_curl_tests.sh
    ```  
    ausgeführt werden.  

---

## Probleme & Lösungen (Kurz)
- **Problem:** `HttpListener` mit `http://+:` benötigt unter Windows teils Adminrechte.  
  **Lösung:** Verwendung von `http://localhost:8080/` als Fallback oder Start im Admin-Terminal.  

---

## Geschätzter Zeitaufwand
- Grundimplementierung Router / Server / Controller: ~25h  
- Repositories & Authentifizierung: ~6h  
- Tests & Debugging / Demo-Skript: ~2h  
- Dokumentation & Protokoll: ~1h  

---

## Weiteres / Nächste Schritte (für Final)
- Austausch der In-Memory-Repositories durch PostgreSQL-Implementierungen (via Npgsql).  
- Erweiterungen: Favoriten, Leaderboard, Recommendations, Moderation UI.  

---

## Link to Git
[https://github.com/if24b267/MRP_SWEN1.git](https://github.com/if24b267/MRP_SWEN1.git)
