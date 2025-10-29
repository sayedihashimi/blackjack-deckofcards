# BlackjackRazor Implementation Plan
Version: 0.1 (Draft)
Date: 2025-10-28
Status: Planning
Related Spec: `speckit.blackjackrazor.spec.md`

## 1. Goal
Implement a .NET 9 Razor Pages web application "BlackjackRazor" per specification, ensuring reliable card rendering via Deck of Cards API, accurate Blackjack rules (Bicycle), responsive dark casino UI, persistent stats, and verifiable acceptance criteria.

## 2. Guiding Constraints
- Root-level projects only (no nested solutions). Initial: `BlackjackRazor` (+ optional `BlackjackRazor.Tests`).
- Razor Pages (not MVC controllers except minimal custom endpoints if needed).
- Tailwind CSS build pipeline (PostCSS) producing a single minimized CSS file; dark casino theme.
- Strict image visibility safeguards (see Section 11).
- CSP restricted to allow remote HTTPS images (no blockage of deckofcardsapi.com) with `img-src 'self' https: data:`.
- Game logic isolated in pure C# services to maximize testability.
- EF Core Sqlite for persistence; migrations included.

## 3. Assumptions & Clarifications
| Topic | Decision/Assumption |
|-------|---------------------|
| Dealer soft 17 | Dealer HITS soft 17 (Bicycle rules). Config flag available; default = HitSoft17 = true. |
| Max splits | Allow up to 3 total hands (i.e., 2 splits). Configurable `MaxHandsAfterSplit = 4` including original. |
| Split Aces rule | Split aces receive one additional card each; cannot hit further. |
| Double after split | Allowed only on non-ace split hands. |
| Currency/decimals | Use `decimal` with 2-place formatting; no localization. |
| Starting bankroll | 5000 (configifiable in appsettings). |
| Min/Max bet | Min 1, Max 5000 (config). |
| Session lifetime | Sliding 30 minutes inactivity; uses server session + auth cookie. |
| Tailwind build | NPM-based dev dependency locally committed config, not the generated CSS. |

## 4. High-Level Architecture
- Presentation: Razor Pages + Partial Components.
- Application Layer: Game Orchestrator (`IGameService`) controlling round flow, splits, double, dealer finish.
- Domain/Logic: Hand evaluator, deck/shoe manager, payout calculator.
- Infrastructure: `DeckApiClient` (typed HttpClient + retry), `AppDbContext` (User, Game, Hand), repositories.
- Persistence: Sqlite file `blackjack.db` at root or `./data` folder (ensure path consistent for hosting).
- Session: Combination of ASP.NET Session + serialized `GameState` (JSON) or strongly typed via a session accessor abstraction.

## 5. Project Layout
```
/BlackjackRazor           (Razor Pages project)
  /Pages
    /Auth (SignIn)
    /Game (Play, New, Debug/State)
  /Services
  /Domain
  /Infrastructure
  /wwwroot
    /css (tailwind input/output)
    /js
  tailwind.config.cjs
  postcss.config.cjs
  package.json
/BlackjackRazor.Tests     (xUnit or MSTest)
```

## 6. EF Core Data Model & Migrations
Entities:
- User (Id PK, Username unique, CreatedUtc)
- Game (Id PK, FK UserId, DeckCount, DeckId, StartedUtc, EndedUtc, DefaultBet, BankrollStart, BankrollEnd, HandsPlayed, PlayerBlackjacks, DealerBlackjacks, PlayerBusts, DealerBusts, PlayerWins, DealerWins, Pushes)
- Hand (Id PK, FK GameId, HandIndex, Bet, Outcome, Payout, WasSplit, WasDouble, PlayedUtc)

Migration Strategy:
1. InitialCreate migration added right after scaffolding.
2. Keep model stable for v1; any additional columns require new migration (tracked in CHANGELOG).

## 7. Core Game Engine Components
| Component | Responsibility |
|-----------|----------------|
| ShoeManager | Create & shuffle multi-deck shoe; detect exhaustion; auto-refresh. |
| HandEvaluator | Compute totals, soft/hard, bust, blackjack. |
| DealerLogic | Execute dealer draw loop respecting HitSoft17 flag. |
| PayoutService | Determine outcomes & payouts per hand. |
| RoundResolver | Orchestrate end-of-round settlement + stats update. |
| GameStateSerializer | Serialize/deserialize session GameState. |
| GameService (IGameService) | Public API: NewGame, Deal, Hit, Stand, Split, Double, Advance, DebugState. |

## 8. GameState Lifecycle
1. NewGame: create deck via API → store DeckId + config (DeckCount, DefaultBet, Bankroll initial).
2. Deal: Validate bet; withdraw from bankroll; draw 2 player + 2 dealer (dealer[1] hidden flag stored separately). Evaluate immediate blackjack conditions.
3. Player Actions: Mutate only active hand. After each action: auto-advance if bust/21/completed.
4. Split: Duplicate bet; move one card to new hand; draw one card to each; re-evaluate (restrictions for Aces).
5. Double: Double bet; draw one card; mark complete.
6. Transition: When all hands complete → reveal dealer hole → dealer play loop (delayed). Record per-hand outcomes.
7. Settlement: Adjust bankroll (refund pushes, add wins, etc.), update session stats & persist summary to DB.
8. Next Round: Reset round-related fields but preserve bankroll & stats.

## 9. Action Validation Rules (Summary)
- Hit: active hand not complete, total < 21, not split ace restricted.
- Stand: active hand.
- Double: hand card count == 2, not already doubled, bankroll >= bet, not split ace restricted.
- Split: hand card count == 2, ranks equal, total hands < MaxHandsAfterSplit, bankroll >= bet.

## 10. Resilience & HTTP Strategy
- Typed client `DeckApiClient`: `CreateShuffledDeckAsync(n)`, `DrawAsync(deckId, count)`.
- Polly-like custom lightweight retry (manual) for Draw: up to 2 retries on transient 5xx/timeout.
- Auto shoe refresh when API indicates 0 remaining (retain DeckCount, new deckId).
- Logging of failures to console (dev) and minimal abstraction for future structured logging.

## 11. Card Rendering Safeguards (MANDATORY CHECKLIST)
| Safeguard | Implementation |
|-----------|----------------|
| Direct remote image URL | `<img src="@card.Image" ...>` (no proxy / rewriting). |
| Eager loading | `loading="eager"` attribute. |
| Flexible sizing | Classes: `h-32 w-auto sm:h-24 md:h-28 lg:h-32 object-contain`. |
| No overflow clipping | Containers use `overflow-visible` and `flex flex-wrap gap-3`. |
| CSP allow images | Middleware sets: `Content-Security-Policy: img-src 'self' https: data:`. |
| Debug test image | `/Debug/State` includes `AS.png` remote test img & fallback warning. |
| Hidden card local back | Styled `<div>` (gradient), replaced on reveal. |
| Mobile visibility | Wrapping flex rows, reduced gaps on small screens. |

## 12. Tailwind & Styling Plan (Revised: CDN First)
Phase 1 (current): Use Tailwind CDN for speed of initial delivery.

CDN Inclusion (in `_Layout.cshtml` `<head>`):
```
<script src="https://cdn.tailwindcss.com"></script>
<script>
  tailwind.config = {
    darkMode: 'class',
    theme: {
      extend: {
        colors: {
          table: {
            felt: '#0f3d2e',
            edge: '#062017'
          }
        }
      }
    }
  };
</script>
```

Custom Utility Layer:
- Add inline `<style>` (temporary) for casino accents (glow, card shadow) until local build pipeline added.

Future Phase Upgrade (when needed):
- Replace CDN with local build (postcss + tailwind) without altering class names.
- Introduce purge (content scanning) only after core pages stable.

Rationale:
- Faster iteration; defers node toolchain complexity.
- Meets requirement to prioritize visible cards & gameplay quickly.

Risks & Mitigation:
- CDN outage → Minimal inline fallback style for critical layout (flex rows, dark background).
- Unpurged CSS size → Acceptable for early dev; schedule optimization before production hardening.

## 13. Razor Pages & Navigation
| Page | Path | Purpose |
|------|------|---------|
| SignIn | `/Auth/SignIn` | Username entry; sets auth cookie & initializes stats. |
| New Game | `/Game/New` | Configure decks and default bet; Create GameState. |
| Play | `/Game/Play` | Active gameplay UI; action forms or AJAX endpoints. |
| Debug State | `/Game/Debug/State` | Dev-only JSON + image diagnostics (conditional compile). |

## 14. Authentication & Session
- Minimal cookie auth: custom claims principal storing Username + UserId.
- Middleware ensures authenticated access to `/Game/*` except SignIn.
- Session uses a constant key (e.g., `GameState`) storing serialized state.
- Defensive null rehydration (if missing, redirect to New Game page).

## 15. Persistence Workflow
- On SignIn: Upsert User.
- On NewGame: Insert new Game row with starting metrics.
- On Round Settlement: Update Game: HandsPlayed++, bankroll end, outcome rollups.
- On Final (optional end session) or inactivity: Mark EndedUtc.
- Each hand resolution: Insert Hand row with snapshot stats.

## 16. Statistics Updates
SessionStats update per hand after settlement. Derived fields:
- NetProfit = Bankroll - BankrollStart.
- Win/Loss classification per hand added to cumulative counts.

## 17. End-of-Round Banner Content
- Hand summary lines: `Hand # (Bet 100) → Win +100 (Total 20 vs Dealer 18)`.
- Aggregate: `Round Net: +150 | Bankroll: 5150`.
- Dismiss or auto-hide on next action.

## 18. Testing Strategy & Matrix
| Layer | Tests |
|-------|-------|
| Unit | HandEvaluator (ace permutations), DealerLogic (soft 17), PayoutService (blackjack 3:2, push), Split/Double validations. |
| Integration | Full round flows (blackjack, split, double, dealer bust). Minimal Razor Page handler tests. |
| Rendering (Optional) | bUnit or Playwright smoke ensuring image `AS.png` loads & hidden card replaced. |
| Performance | Simulated 5k rounds for memory stability (optional gating). |

Mapping to Acceptance (sample):
| AC | Test(s) |
|----|---------|
| Cards visible | Playwright image load + Debug page check |
| Dealer hidden then reveal | Integration scenario DealerRevealSequence |
| Valid actions only | Unit validation tests + UI disable state test |
| Split independent buttons/results | Integration SplitFlow test |
| End round banner | UI render assertion |
| Mobile no scroll | Viewport snapshot test (heuristic) |

## 19. Phased Roadmap & Tasks
### Phase 1: Scaffold & Infrastructure
- Initialize Razor Pages project (.NET 9).
- Add Tailwind pipeline (package.json, configs, npm build).
- Add EF Core Sqlite, create Initial migration + DB.
- Configure Program.cs (session, auth, CSP, static files, typed client).

### Phase 2: Domain & Engine
- Implement Card/Hand models, HandEvaluator, ShoeManager, DealerLogic, PayoutService.
- Unit tests for evaluator & payouts.

### Phase 3: Game Service & State
- Implement `IGameService` orchestration & session serialization.
- Add action validation helpers with tests.

### Phase 4: Razor Pages UI
- SignIn, New Game, Play layout per mockup (static first, then dynamic binding).
- Card rendering & hidden card component.
- Action buttons with contextual enablement.

### Phase 5: Round Resolution & Stats
- Implement settlement, bankroll adjustment, stats update logic.
- Persist Game + Hand rows.

### Phase 6: Debug & Diagnostics
- `/Debug/State` page + warning if test image fails.
- Logging instrumentation (dev only).

### Phase 7: Polish & Responsive
- Mobile layout tuning (flex wrap, card scaling).
- End-of-round banner & animations.

### Phase 8: Final Testing & Hardening
- Full test matrix execution.
- Coverage review; add missing critical tests.
- Performance smoke (optional).

## 20. Detailed Task Breakdown (Backlog Style)
1. Create solution & Razor Pages project.
2. Add Tailwind pipeline (config, build script, base styles).
3. Implement CSP middleware.
4. Register session & cookie auth pipeline & SignIn page.
5. Add EF Core context + migrations.
6. Implement domain models & evaluation logic.
7. Implement `DeckApiClient` with retry.
8. Implement `ShoeManager` (reuse deck draws, auto-refresh).
9. Implement `IGameService` (NewGame, Deal, Hit, Stand, Split, Double, AdvanceDealer, SettleRound).
10. Implement payout logic & stats update.
11. Build UI structure for Play page and partials (dealer row, player hand component, actions panel, stats sidebar, round banner).
12. Bind actions to handlers (form posts or minimal AJAX using fetch).
13. Implement hidden dealer card reveal mechanism.
14. Add `/Debug/State` page.
15. Add unit tests (Evaluator, Dealer, Payout, Validation).
16. Add integration tests (TypicalRound, BlackjackRound, SplitRound, DoubleRound, DealerBustRound).
17. Add image visibility test (optional Playwright or fallback stub until infra ready).
18. Tune responsiveness & no-scroll guarantee (viewport testing).
19. Final acceptance checklist + README updates.

## 21. Risk & Mitigation
| Risk | Impact | Mitigation |
|------|--------|-----------|
| CSP misconfigured blocks images | Blank cards | Automated dev check on `/Debug/State` + manual header test. |
| Deck API downtime | Gameplay break | Auto retry + shoe prefetch; fallback error message. |
| Session loss mid-round | User confusion | Detect null session & show recovery prompt (offer New Game). |
| Race conditions on split/double | Incorrect payouts | Centralize state mutations in `IGameService` with lock (if concurrency emerges). |
| Mobile overflow | Hidden cards or buttons | Early viewport design & wrap-based layout + QA.

## 22. Acceptance Criteria Traceability (Initial)
Will extend into `docs/traceability.md` once tests implemented. Current mapping listed in Section 18.

## 23. Deliverables Checklist
| Item | Status (Planned) |
|------|------------------|
| Razor Pages project compiles | Phase 1 |
| Tailwind CSS build outputs site.css | Phase 1 |
| DB migration & sqlite file | Phase 1 |
| Game engine logic unit tested | Phase 2 |
| Action enablement rules enforced | Phase 4/5 |
| Debug page operational | Phase 6 |
| End-of-round banner | Phase 7 |
| Responsive mobile layout | Phase 7 |
| Test matrix green | Phase 8 |

## 24. Exit Criteria for v1 Release
- All acceptance criteria validated.
- Code coverage thresholds (logic ≥ 90%).
- No P1 defects open.
- README documents setup/run steps.

## 25. Next Steps After Plan Approval
1. Execute Phase 1 scaffold & commit.
2. Establish continuous integration (GitHub Actions) for build + test + tailwind build.
3. Implement domain engine & unit tests.

---
End of Plan.
