You are generating a .NET 9 Razor Pages web app called "BlackjackRazor" that lets users play Blackjack using the public Deck of Cards API (https://deckofcardsapi.com/). Follow the spec below exactly.

# High-level
- Tech: .NET 9, Razor Pages (no MVC controllers), C#, minimal hosting, HttpClient typed client, EF Core Sqlite with migrations.
- Card data & images come from Deck of Cards API responses; do NOT bundle images locally.
- Simple open sign-in: user enters a string "username" to sign in; no passwords.
- Track session stats in-memory per visitor AND persist cumulative stats to Sqlite via EF.
- Scope of game: standard Blackjack with Hit/Stand; no betting/money; compute hand values (Aces 1 or 11); dealer hits until 17 or more (treat soft-17 as the dealer HITS until total >= 17).
- Show live session stats on the game page; also show historical stats for the signed-in user on a Stats page.

# Project structure
Files to create/edit:
- /Program.cs: Minimal hosting, Razor Pages, cookie auth with a custom username-only sign-in page, session state, HttpClient typed client for Deck API, EF Core Sqlite DbContext registration.
- /appsettings.json: Connection string "DefaultConnection" pointing to "Data Source=blackjack.db"
- /Data/AppDbContext.cs: EF Core DbContext.
- /Data/Entities.cs:
  - User { int Id; string Username (unique); DateTime CreatedUtc }
  - Game { int Id; int UserId; int DeckCount; string DeckId; DateTime StartedUtc; DateTime? EndedUtc; string Result; int HandsPlayed; int PlayerBlackjacks; int DealerBlackjacks; int PlayerBusts; int DealerBusts; int PlayerWins; int DealerWins; int Pushes; User User }
- (Optional) /Data/Mappings.cs if you prefer Fluent API.
- /Services/DeckApiClient.cs: Typed HttpClient wrapper for DeckOfCards API:
  - Task<string> CreateShuffledDeckAsync(int deckCount)
    - GET /api/deck/new/shuffle/?deck_count={n} -> return deck_id
  - Task<List<CardDto>> DrawAsync(string deckId, int count)
    - GET /api/deck/{deck_id}/draw/?count={count}
  - CardDto { string Code; string Image; string Value; string Suit }
- /Services/BlackjackEngine.cs: Pure logic to evaluate totals, detect blackjack/busts, dealer play, hand outcome.
- /Models/GameModels.cs:
  - GameState (kept in session): { string DeckId; int DeckCount; List<CardDto> Player; List<CardDto> Dealer; bool IsPlayerTurn; bool IsGameOver; string Outcome; }
  - SessionStats: { int Hands; int Wins; int Losses; int Pushes; int PlayerBlackjacks; int DealerBlackjacks; int PlayerBusts; int DealerBusts; double WinRate => (Hands==0?0:Wins*100.0/Hands) }
- Razor Pages:
  - /Pages/Index.cshtml: Landing; if not signed in, link to SignIn; if signed in, link to New Game / Continue / Stats.
  - /Pages/Auth/SignIn.cshtml: Form with one input "username". On POST, create cookie principal with Name = username; if new user, insert User row.
  - /Pages/Auth/SignOut.cshtml: Clears auth cookie and session; redirect Home.
  - /Pages/Game/New.cshtml: Choose deck count (1–8), POST creates deck via DeckApiClient, initializes GameState in session, creates Game row, redirects to Play.
  - /Pages/Game/Play.cshtml: Shows player and dealer cards (using Image URLs from API), totals, buttons: Hit, Stand (POST handlers). When player stands or busts, perform dealer play, compute outcome, update SessionStats + Game row, and allow "New Hand" (re-uses same deckId until exhausted).
  - /Pages/Stats/Index.cshtml: Shows session stats and user historical aggregate from DB.
  - Shared layout with simple nav: Home | New Game | Play | Stats | Sign Out (if signed in).
- /wwwroot/css/site.css: Minimal clean styling.

# Authentication (username-only)
- Use cookie auth (no Identity). SignIn page:
  - POST creates ClaimsPrincipal with Name = username (trimmed, non-empty).
  - Store user in DB if not exists (unique Username).
  - Set auth cookie and redirect to Home.
- Add [Authorize] to all Game and Stats pages; leave Index and SignIn open.

# Session & state
- Use session to store GameState and SessionStats keyed per user session.
- When a hand completes, update SessionStats in session AND persist aggregates into the current Game row (and if desired, roll-up into user totals for quick query).

# EF Core / Sqlite
- Packages to add:
  - Microsoft.EntityFrameworkCore.Sqlite
  - Microsoft.EntityFrameworkCore.Design
- Migrations and DB update commands (one per line; no &&):
  - dotnet tool install --global dotnet-ef
  - dotnet add package Microsoft.EntityFrameworkCore.Sqlite
  - dotnet add package Microsoft.EntityFrameworkCore.Design
  - dotnet ef migrations add InitialCreate
  - dotnet ef database update

# Blackjack rules (implement in BlackjackEngine)
- Card values: 2–10 = face value, J/Q/K = 10, Ace = 11 unless bust; then downgrade Aces (11→1) until total <= 21 or no Aces.
- Blackjack: initial 2-card 21.
- Player turn: Hit or Stand. If player busts, dealer doesn’t play.
- Dealer artificial play: draw until total >= 17 (treat soft-17 as HIT until >= 17).
- Outcomes: PlayerWin, DealerWin, Push, PlayerBlackjack, DealerBlackjack, PlayerBust, DealerBust.
- No split/double/insurance/bets.

# Deck of Cards API integration
- On New Game: call CreateShuffledDeckAsync(deckCount) and save deck_id to GameState + DB.
- On deal: DrawAsync(deckId, count). Use returned "image" URLs directly in <img>.
- Handle deck exhaustion gracefully: if draw fails due to remaining = 0, create a new shuffled deck with the same deckCount and continue.

# Razor Pages handlers
- Game/New: GET shows form; POST starts game, inserts Game row with StartedUtc, DeckCount, DeckId.
- Game/Play: GET shows current hand; POST Hit draws 1 for player; if bust → finalize.
- Game/Play: POST Stand runs dealer play; finalize outcome; update Game row counts; mark EndedUtc if you decide to end the “game session” after N hands or leave open across hands.
- Provide a POST “New Hand” that clears player/dealer lists, deals two cards to each, IsPlayerTurn=true.

# Data persistence expectations
- Persist per-Game aggregates to Game table (HandsPlayed, Wins/Losses/Pushes, Blackjacks, Busts).
- For “Stats/Index”, show:
  - SessionStats live counters
  - Historical totals aggregated from DB for the signed-in user (SUM across Game rows).

# Program.cs specifics
- AddRazorPages(), AddSession(), AddAuthentication(CookieScheme).AddCookie(...) with login path /Auth/SignIn.
- UseAuthentication(), UseAuthorization(), UseSession(), MapRazorPages().
- Register DeckApiClient as a typed client using HttpClientFactory with BaseAddress = https://deckofcardsapi.com/.
- Register AppDbContext with Sqlite and connection string from config.

# UI/UX
- Clean, minimal layout. On Play page, show:
  - Dealer section: cards + total (hide dealer’s hole card value until player stands).
  - Player section: cards + total; action buttons (Hit, Stand).
  - Sidebar/panel showing SessionStats (Hands, Wins, Losses, Pushes, WinRate, Player/Dealer Blackjacks, Player/Dealer Busts).
- Show toast/message for round outcome and a “New Hand” button once round is over.

# Validation & testing hooks
- Guard: reject empty usernames; trim whitespace.
- Null checks around API calls; show simple error if API unavailable.
- Unit-test BlackjackEngine totals and outcome logic with a few targeted cases.

# Build & run
- dotnet new webapp -n BlackjackRazor
- cd BlackjackRazor
- dotnet add package Microsoft.EntityFrameworkCore.Sqlite
- dotnet add package Microsoft.EntityFrameworkCore.Design
- dotnet tool install --global dotnet-ef
- dotnet ef migrations add InitialCreate
- dotnet ef database update
- dotnet run

# Acceptance Criteria
- I can sign in with just a username.
- I can create a new game selecting deck count.
- I can play Hit/Stand; images render from Deck of Cards API.
- Session stats update in real time; a Stats page shows both session and historical totals.
- A Sqlite DB file “blackjack.db” is created and updated via EF migrations/updates.
- No commands chained with &&. Use one command per line.
- Code compiles and runs with .NET 9.
