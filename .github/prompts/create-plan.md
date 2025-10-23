You are generating a .NET 9 Razor Pages web app called "BlackjackRazor" that lets users play Blackjack using the public Deck of Cards API (https://deckofcardsapi.com/). Follow the spec below exactly.

# High-level
- Tech: .NET 9, Razor Pages (no MVC controllers), C#, minimal hosting, HttpClient typed client, EF Core Sqlite with migrations.
- Card data & images come from Deck of Cards API responses; do NOT bundle images locally.
- Language: English. Set `<html lang="en" class="dark">` and default to a dark theme.
- Styling: Tailwind CSS for all UI; configure dark mode as default (apply the `dark` class at root).
- Simple open sign-in: user enters a string "username" to sign in; no passwords.
- Rules: Implement Blackjack per Bicycle’s rules at https://bicyclecards.com/how-to-play/blackjack (use those rules as the source of truth for hand values, dealer behavior, blackjack payout, etc.). Support valid actions: **Hit, Stand, Split, Double Down**. No insurance/side bets unless explicitly added later.
- Default shoe: **6 decks** (user can override at New Game).
- Betting: user sets a **default bet** when creating a game; can **set the bet amount per hand** before dealing. Track bankroll and payouts per hand.
- Track session stats in-memory per visitor AND persist historical stats to Sqlite via EF.

# Project structure
Files to create/edit:
- /Program.cs: Minimal hosting, Razor Pages, cookie auth with a username-only sign-in page, session state, typed HttpClient for Deck API, EF Core Sqlite DbContext registration, Tailwind static files.
- /appsettings.json: Connection string "DefaultConnection" pointing to "Data Source=blackjack.db"
- /tailwind.config.js and PostCSS setup; include Tailwind in /wwwroot/css/site.css.
- /Data/AppDbContext.cs
- /Data/Entities.cs:
  - User { int Id; string Username (unique); DateTime CreatedUtc }
  - Game { int Id; int UserId; int DeckCount; string DeckId; DateTime StartedUtc; DateTime? EndedUtc; decimal DefaultBet; decimal BankrollStart; decimal BankrollEnd; int HandsPlayed; int PlayerBlackjacks; int DealerBlackjacks; int PlayerBusts; int DealerBusts; int PlayerWins; int DealerWins; int Pushes; User User }
  - Hand { int Id; int GameId; int HandIndex; decimal Bet; string Outcome; decimal Payout; bool WasSplit; bool WasDouble; DateTime PlayedUtc; Game Game }
- /Services/DeckApiClient.cs: Typed HttpClient wrapper:
  - Task<string> CreateShuffledDeckAsync(int deckCount) => GET /api/deck/new/shuffle/?deck_count={n} → deck_id
  - Task<List<CardDto>> DrawAsync(string deckId, int count) => GET /api/deck/{deck_id}/draw/?count={count}
  - CardDto { string Code; string Image; string Value; string Suit }
- /Services/BlackjackEngine.cs: Pure rules engine (no HTTP) implementing Bicycle rules & actions. Responsibilities:
  - Tally with Aces (11→1 downgrades), Blackjack detection, busts.
  - Dealer play per Bicycle rules.
  - Split: allow split of pairs; play multiple player hands sequentially; handle draw rules for split aces per Bicycle rules.
  - Double Down: restrict timing per Bicycle rules; apply one-card draw then stand.
  - Payouts per Bicycle rules (e.g., blackjack pays 3:2); compute `Outcome` and `Payout`.
- /Models/GameModels.cs:
  - GameState (session): { string DeckId; int DeckCount; decimal Bankroll; decimal CurrentBet; decimal DefaultBet; bool IsDealing; int ActiveHand; List<PlayerHand> PlayerHands; List<CardDto> Dealer; bool IsRoundOver; string RoundMessage; }
  - PlayerHand: { List<CardDto> Cards; bool IsComplete; bool IsBust; bool IsBlackjack; bool IsDoubled; bool IsSplit; }
  - SessionStats: { int Hands; int Wins; int Losses; int Pushes; int PlayerBlackjacks; int DealerBlackjacks; int PlayerBusts; int DealerBusts; double WinRate; decimal NetProfit; }
- Razor Pages:
  - /Pages/Index.cshtml: Landing; if not signed in, link to SignIn; if signed in, link to New Game / Continue / Stats.
  - /Pages/Auth/SignIn.cshtml: One input "username" → cookie principal; create User row if new.
  - /Pages/Auth/SignOut.cshtml: Clears auth cookie and session; redirect Home.
  - /Pages/Game/New.cshtml: Form with deck count (default 6) and **Default Bet**; POST creates deck via DeckApiClient, initializes GameState (BankrollStart = some sensible default like 1000 unless provided), creates Game row, redirects to Play.
  - /Pages/Game/Play.cshtml: Dark UI using Tailwind; sections:
    - Controls: Current Bet input, Set/Deal button, then action buttons: **Hit, Stand, Split, Double Down** (enable/disable contextually).
    - Player area: one column per hand (if split), show cards and total; mark active hand.
    - Dealer area: show cards and total; hide hole card until player concludes all hands.
    - Sidebar/panel: SessionStats (Hands, Wins, Losses, Pushes, WinRate, Player/Dealer Blackjacks, Player/Dealer Busts, NetProfit) + current bankroll.
    - Outcome toast with payouts; buttons **New Hand** (re-uses shoe) and **End Game**.
  - /Pages/Stats/Index.cshtml: Shows session stats and historical aggregates from DB (by user).
- Shared layout: Tailwind-powered dark layout, nav: Home | New Game | Play | Stats | Sign Out (if signed in). Ensure `<html lang="en" class="dark">`.

# Authentication (username-only)
- Cookie auth (no Identity). POST SignIn: trimmed, non-empty username → ClaimsPrincipal(Name=username); unique Username in DB.

# Session, betting & bankroll
- Store GameState + SessionStats in session.
- On each hand:
  - Use `CurrentBet` (if not set, use `DefaultBet` from Game).
  - Deduct bet from bankroll when hand starts; apply payout on resolution (e.g., blackjack 3:2).
  - Persist a Hand row with Bet/Outcome/Payout.
- On **Split**: create new PlayerHand with one card each hand; handle draw limits per Bicycle rules (e.g., split aces restrictions).
- On **Double Down**: double bet, draw exactly one card, mark hand complete.

# EF Core / Sqlite
- Packages:
  - Microsoft.EntityFrameworkCore.Sqlite
  - Microsoft.EntityFrameworkCore.Design
- Migrations and DB update (one per line; no &&):
  - dotnet tool install --global dotnet-ef
  - dotnet add package Microsoft.EntityFrameworkCore.Sqlite
  - dotnet add package Microsoft.EntityFrameworkCore.Design
  - dotnet ef migrations add InitialCreate
  - dotnet ef database update

# Deck of Cards API integration
- On New Game: call CreateShuffledDeckAsync(deckCount) and save deck_id to GameState + DB.
- On deal/draws: DrawAsync(deckId, count); use returned "image" URLs directly in `<img>`.
- If shoe is exhausted (remaining=0), create a fresh shuffled shoe with the same deck count and continue.

# Razor Pages handlers
- Game/New: GET shows form with DeckCount (default 6) and DefaultBet; POST starts game, inserts Game row with StartedUtc, DeckCount, DeckId, DefaultBet, BankrollStart.
- Game/Play:
  - GET shows current state.
  - POST Deal initializes a new round: two cards to player (two per hand if already split), two to dealer (one hidden).
  - POST Hit/Stand/Split/Double: apply BlackjackEngine, progress active hand(s); when all hands done, run dealer play, resolve outcomes/payouts, update SessionStats + Game aggregates + Hand rows. Update Bankroll and Game.BankrollEnd.
  - POST New Hand: reset hand state but keep shoe, bankroll, stats.
  - POST End Game: set Game.EndedUtc; redirect Stats.
- Stats/Index: SessionStats live + historical aggregates (SUM on Hand.Payout, counts).

# BlackjackEngine essentials
- Implement exactly per Bicycle rules at https://bicyclecards.com/how-to-play/blackjack:
  - Tally rules incl. Aces.
  - Dealer behavior per Bicycle.
  - Blackjack detection & **3:2 payout**.
  - Valid actions: Hit, Stand, Split (pairs), Double Down (timing per Bicycle).
  - Split-hand and split-aces nuances; double-down constraints; payout math for each hand.
  - Outcomes: PlayerWin, DealerWin, Push, PlayerBlackjack, DealerBlackjack, PlayerBust, DealerBust.
- Provide unit tests for totals, dealer play, split/double, and payout math.

# Program.cs specifics
- AddRazorPages(), AddSession(), AddAuthentication(Cookie).AddCookie(loginPath: "/Auth/SignIn").
- UseAuthentication(), UseAuthorization(), UseSession(), MapRazorPages().
- Register DeckApiClient via HttpClientFactory with BaseAddress = https://deckofcardsapi.com/.
- Register AppDbContext with Sqlite using "DefaultConnection" from config.
- Serve Tailwind-compiled CSS from wwwroot.

# UI/UX (Tailwind, dark by default)
- Responsive, dark-themed layout; cards shown with image URLs; active-hand highlighting; disabled states for invalid actions.
- Show clear bet controls (numeric input) and bankroll changes after each resolution.
- Toast/banner for round outcomes and concise result per hand (including payout).

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
- `<html lang="en" class="dark">` present; Tailwind used for all styling; dark theme by default.
- Default shoe is 6 decks (overridable at New Game).
- Users can sign in with just a username.
- Users can set a default bet at game creation and a per-hand bet before dealing.
- Play supports Hit, Stand, **Split**, **Double Down** with Bicycle-consistent behavior and payouts.
- Images render from Deck of Cards API.
- Session stats update live; Stats page shows historical aggregates (wins/losses/pushes, blackjacks, busts, net profit).
- Sqlite DB “blackjack.db” created and updated via EF migrations/updates.
- No commands chained with &&. Code compiles and runs with .NET 9.
