You are generating a .NET 9 Razor Pages web app called "BlackjackRazor" that lets users play Blackjack using the public Deck of Cards API ([https://deckofcardsapi.com/](https://deckofcardsapi.com/)). Follow the spec below exactly.

# General Build Instructions

* Each **new project** must be created in its **own folder** and **cannot be nested** inside another project folder.
  This includes **test projects** (e.g., `BlackjackRazor.Tests`).
* After generating code, **verify the project builds successfully** with no compiler errors.
* If any **test cases** are created, **execute them** and confirm **all pass**.
* The web app must:

  * Use **English** (`<html lang="en" class="dark">`).
  * Use **Tailwind CSS** for styling.
  * Default to a **dark casino-themed UI** (deep greens, gold accents, soft shadows, rounded cards/buttons).
  * Use **buttons for all important actions** (Hit, Stand, Split, Double Down, Deal, New Hand, End Game).
  * Be **fully responsive**: when viewed on a phone, **all cards and buttons must be visible without scrolling**.
  * Follow the visual layout and styling shown in the file **`assets\\blackjack-mockup.png`** for the Play Blackjack page (`/Pages/Game/Play.cshtml`). Match the overall layout, spacing, button positions, colors, and visual hierarchy as closely as possible using Tailwind.

# High-level

* Tech: .NET 9, Razor Pages (no MVC), minimal hosting, HttpClient typed client, EF Core Sqlite with migrations.
* Card data & images come from Deck of Cards API responses; do NOT bundle locally.
* Implement full Blackjack per Bicycle rules ([https://bicyclecards.com/how-to-play/blackjack](https://bicyclecards.com/how-to-play/blackjack)).
* Support actions: **Hit**, **Stand**, **Split**, **Double Down**.
* Default to **6 decks** per shoe (user can override on new game creation).
* Allow user to specify **default bet** when creating a game and **custom bet per hand** before dealing.
* Use cookie-based sign-in with a **username only**, no password.
* Track **session stats in memory** and **historical stats in Sqlite** via EF Core.

# Project Structure

* Create projects in top-level folders:

  * `BlackjackRazor` — main Razor Pages web app.
  * `BlackjackRazor.Tests` — optional test project for BlackjackEngine and payout logic.
* Files to include:

  * **Program.cs** — Razor Pages setup, cookie auth, session state, EF Sqlite, HttpClient for Deck API.
  * **appsettings.json** — connection string to `"Data Source=blackjack.db"`.
  * **/Data/AppDbContext.cs**, **Entities.cs**, **Mappings.cs** if needed.
  * **/Services/DeckApiClient.cs** — Typed client for Deck of Cards API.
  * **/Services/BlackjackEngine.cs** — Implements Bicycle rules (Hit, Stand, Split, Double Down, Aces handling, payouts).
  * **/Models/GameModels.cs** — GameState, PlayerHand, SessionStats, etc.
  * **/Pages/** — Razor Pages for SignIn, SignOut, Game/New, Game/Play, Stats, etc.
  * **/wwwroot/css/site.css**, **tailwind.config.js** — Tailwind config with dark theme enabled by default.
  * **/assets/blackjack-mockup.png** — reference design for Play page.

# Gameplay Rules

* Follow **Bicycle Blackjack rules** strictly.
* Only enable actions that are currently valid:

  * **Split** only when first two cards are of the same rank.
  * **Double Down** only on first move per hand.
  * **Stand** and **Hit** enabled/disabled as appropriate.
* Dealer must **draw to 16** and **stand on all 17s**, treating **soft 17 as a hit** until total ≥ 17.
* Payouts per Bicycle:

  * Blackjack pays **3:2**.
  * Win pays **1:1**.
  * Push returns the bet.
* Bets and bankroll:

  * Deduct bet when hand starts.
  * Apply payouts when hand resolves.
  * Persist results to database.

# User Interface (Tailwind + Dark Casino Theme)

* Use Tailwind CSS classes for all styling.
* Dark mode enabled by default.
* Match layout and style from `assets\\blackjack-mockup.png`:

  * Dealer area at top, cards horizontally aligned.
  * Player area(s) below dealer, one section per hand (when split).
  * Buttons arranged horizontally below the player area (Hit, Stand, Split, Double Down, New Hand, End Game).
  * Stats panel on the side or bottom (as per mockup).
  * Use Tailwind classes like `flex`, `justify-center`, `gap-4`, `bg-green-950`, `border-yellow-500`, `rounded-xl`, and glow/hover effects.
  * Buttons: large, gold-outlined, glowing hover state for active actions.
  * Text and highlights use gold/yellow (`text-yellow-400`, `border-yellow-500`).
  * Add subtle casino effects: gradients, inner shadows, glowing borders.
* Ensure the page is **fully responsive**:

  * Cards shrink on mobile (`w-24 sm:w-16` or smaller).
  * Buttons wrap into multiple lines if needed.
  * No horizontal scrolling; all game elements visible on small screens.

# Authentication (Username-only)

* Cookie-based, no Identity framework.
* On SignIn:

  * User enters a username.
  * Create ClaimsPrincipal(Name=username).
  * Add new User to DB if not exists.
  * Redirect to home.
* On SignOut: clear cookie/session.

# EF Core / Sqlite

* Packages:

  * Microsoft.EntityFrameworkCore.Sqlite
  * Microsoft.EntityFrameworkCore.Design
* Commands (no `&&`):

  ```
  dotnet tool install --global dotnet-ef
  dotnet add package Microsoft.EntityFrameworkCore.Sqlite
  dotnet add package Microsoft.EntityFrameworkCore.Design
  dotnet ef migrations add InitialCreate
  dotnet ef database update
  ```

# Deck of Cards API

* On new game: `GET /api/deck/new/shuffle/?deck_count={n}` → store deck_id.
* On draw: `GET /api/deck/{deck_id}/draw/?count={count}`.
* Use returned image URLs directly in `<img>` tags.
* When deck exhausted, reshuffle automatically with same deck count.

# Testing & Verification

* After generating code:

  * Ensure **no build errors** (`dotnet build` succeeds).
  * If **test project** exists, run all tests (`dotnet test`) — all must pass.
* Include at least unit tests for:

  * Hand value calculation (Ace handling).
  * Dealer logic (soft 17, stand rules).
  * Split & Double Down behavior.
  * Payout computation (Blackjack 3:2, Push, Bust).

# Acceptance Criteria

✅ All projects created in separate folders (no nesting).
✅ Builds with zero compile errors.
✅ All tests pass.
✅ Web app runs successfully and plays Blackjack per Bicycle rules.
✅ Follows layout and design in `assets\\blackjack-mockup.png`.
✅ Fully responsive — all cards & controls visible on phones without scrolling.
✅ Dark Tailwind casino-style theme by default.
✅ Only valid actions are clickable at any time.
✅ Images load from Deck of Cards API.
✅ Sqlite database persists game and user stats.
✅ Uses English language setting.
✅ No chained shell commands (`&&`).
✅ Works cleanly with .NET 9.
