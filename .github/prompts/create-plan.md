You are generating a .NET 9 Razor Pages web app called "BlackjackRazor" that lets users play Blackjack using the public Deck of Cards API ([https://deckofcardsapi.com/](https://deckofcardsapi.com/)). Follow this spec exactly. If anything is ambiguous, choose the option that best guarantees card images render visibly on desktop and mobile.

# Build & Project Rules

* Create projects at the repository root; **no nested projects** (include `BlackjackRazor.Tests` if tests are added).
* After generation: `dotnet build` must succeed and **all tests** (if any) must pass via `dotnet test`.
* Language is **English**; use `<html lang="en" class="dark">`.
* Styling: **Tailwind CSS** (dark, casino theme). Add a minimal Tailwind pipeline and ensure CSS loads in production build.
* No shell command chaining with `&&`.

# Card Visibility Safeguards (MANDATORY)

Implement all of the following to ensure cards render:

1. **DTO & Usage**

   * Deck API DTO: `CardDto { string Code; string Image; string Value; string Suit }`
   * Use `card.Image` (fully-qualified https URL) directly in `<img src="...">`.

2. **HttpClient**

   * Typed client `DeckApiClient` with:

     * `BaseAddress = https://deckofcardsapi.com/`
     * `CreateShuffledDeckAsync(int n)` → `GET api/deck/new/shuffle/?deck_count={n}` returns `deck_id`
     * `DrawAsync(string deckId, int count)` → `GET api/deck/{deck_id}/draw/?count={count}` returns `cards[]`
   * Add transient retry (2 attempts) for `DrawAsync` if HTTP fails.

3. **Static Files + CSP**

   * `app.UseStaticFiles();` **before** routing.
   * Add a permissive CSP for images (only), e.g. middleware to set:

     * `img-src 'self' https: data:` (do NOT block deckofcardsapi.com images).

4. **Razor Image Markup**

   * For each card:

     ```html
     <img src="@card.Image" alt="@card.Code"
          loading="eager"
          class="h-32 w-auto sm:h-24 max-w-full object-contain select-none" />
     ```
   * Never hide cards with `overflow:hidden` containers. Use `overflow-visible` and sufficient `gap`.

5. **Layout Safety**

   * Use a centered container: `max-w-screen-xl mx-auto px-3`
   * Do **not** use absolute positioning for core card rows.
   * Ensure cards are in a visible flex row that wraps: `flex flex-wrap items-start justify-center gap-3`

6. **Diagnostics**

   * Add `/Debug/State` page (dev-only) that prints current `GameState` JSON and a single test `<img>` with a hardcoded URL:
     `https://deckofcardsapi.com/static/img/AS.png`
   * If the test image fails to render, show a warning banner.

# Gameplay Rules (Bicycle)

* Follow: [https://bicyclecards.com/how-to-play/blackjack](https://bicyclecards.com/how-to-play/blackjack)
* Actions: **Hit, Stand, Split, Double Down** (valid only when rules allow).
* Default shoe: **6 decks**, user can override on New Game.
* Betting: default bet at game creation; per-hand bet before dealing; maintain bankroll; blackjack pays **3:2**; win **1:1**; push returns bet.
* Dealer:

  * On initial deal, **second dealer card is hidden**.
  * After player finishes, **reveal** and play out **one card at a time** with a brief delay; draw to 16, hit soft-17 until total ≥ 17.
* Split:

  * Only when first two player cards same rank.
  * Each split hand shows **its own action buttons** and **its own bet & result** next to that hand.

# State & Persistence

* Simple sign-in with username (cookie auth; no Identity).
* Session:

  * `GameState { string DeckId; int DeckCount; decimal Bankroll; decimal DefaultBet; decimal CurrentBet; List<PlayerHand> PlayerHands; List<CardDto> Dealer; bool IsRoundOver; int ActiveHand; string RoundMessage }`
  * `PlayerHand { List<CardDto> Cards; bool IsComplete; bool IsBust; bool IsBlackjack; bool IsDoubled; bool IsSplit; decimal Bet; string? Outcome; decimal Payout }`
  * `SessionStats { int Hands; int Wins; int Losses; int Pushes; int PlayerBlackjacks; int DealerBlackjacks; int PlayerBusts; int DealerBusts; decimal NetProfit }`
* EF Core Sqlite (`blackjack.db`):

  * `User(Id, Username unique, CreatedUtc)`
  * `Game(Id, UserId, DeckCount, DeckId, StartedUtc, EndedUtc?, DefaultBet, BankrollStart, BankrollEnd, HandsPlayed, PlayerBlackjacks, DealerBlackjacks, PlayerBusts, DealerBusts, PlayerWins, DealerWins, Pushes)`
  * `Hand(Id, GameId, HandIndex, Bet, Outcome, Payout, WasSplit, WasDouble, PlayedUtc)`

# Pages & UX (Tailwind, Dark Casino Theme)

* Follow layout of `assets/blackjack-mockup.png` for `/Pages/Game/Play.cshtml`.
* Dealer row (top): cards in a flex row; **second card hidden** until player is done (use a local “card back” CSS block or SVG; don’t fetch external back image).
* Player rows (below): one column per hand when split; each shows **bet**, **action buttons**, and final **result/payout**.
* Controls:

  * **Deal/Bet** input (numeric) before starting a round.
  * During a hand: show **Hit**, **Stand**, **Split**, **Double Down**; enable/disable contextually.
* Session sidebar/panel: Hands, W/L/Push, Blackjacks, Busts, NetProfit, Bankroll.
* End of round: display a **clear banner** (e.g., top sticky or modal) summarizing each hand outcome and **total won/lost** this round.
* Mobile (no scrolling requirement): make all cards & buttons visible without scrolling by:

  * Using `flex-wrap`, `gap-2`, and scaling card height down: `h-24 md:h-28 lg:h-32`
  * Wrapping buttons to a second row when needed.
  * Avoiding fixed heights; allow content to wrap naturally.

# Hidden-Card Implementation (no external asset)

* Render the hidden dealer card as a Tailwind-styled “card back”:

  ```html
  <div class="h-32 sm:h-24 aspect-[71/96] rounded-md bg-gradient-to-br from-slate-700 to-slate-900 border border-slate-500 shadow-inner"></div>
  ```
* Replace this with the actual `<img>` once revealing the dealer’s down card.

# Program.cs

* Minimal hosting; `AddRazorPages()`, `AddSession()`, cookie auth (login path `/Auth/SignIn`).
* `UseHttpsRedirection()`, `UseStaticFiles()`, **CSP img-src `'self' https: data:`**, `UseSession()`, `UseAuthentication()`, `UseAuthorization()`, `MapRazorPages()`.
* Register `DeckApiClient` via `HttpClientFactory` and `AppDbContext` for Sqlite.

# API Exhaustion Handling

* If `remaining == 0` on draw, auto-create a fresh 6-deck shoe (or use current DeckCount) and continue.

# Tests (if you add a test project)

* Engine: totals (Aces), dealer soft-17 behavior, split & double flow, payouts (3:2 BJ, push).
* Simple integration smoke: one “deal” sequence draws 4 images, and `AS` test image is retrievable.

# Commands (one per line; no &&)

* `dotnet tool install --global dotnet-ef`
* `dotnet new webapp -n BlackjackRazor`
* `cd BlackjackRazor`
* `dotnet add package Microsoft.EntityFrameworkCore.Sqlite`
* `dotnet add package Microsoft.EntityFrameworkCore.Design`
* `dotnet ef migrations add InitialCreate`
* `dotnet ef database update`
* `dotnet run`

# Acceptance Criteria

* Cards are **visible** on desktop and mobile; the `/Debug/State` page’s `AS.png` also renders.
* Dealer’s second card is hidden on deal, then revealed; dealer plays out **card-by-card**.
* Only valid actions enabled at any time; split hands each have their own buttons and bet/result display.
* Clear end-of-round banner shows **result** and **money won/lost**.
* Dark Tailwind casino theme; responsive; no scrolling needed on phones for cards + buttons.
* No nested projects; build succeeds; tests (if present) pass; images come from deckofcardsapi.com over HTTPS.
