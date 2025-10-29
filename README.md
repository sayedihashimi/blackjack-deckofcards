# Blackjack Razor

A feature-rich Blackjack web application built with ASP.NET Core Razor Pages (.NET 9), EF Core (Sqlite), Tailwind CSS, and a modular domain/service architecture.

## Features
- Deterministic, testable game engine (`GameService`) with phases: NotStarted → PlayerActing → DealerActing → Settled.
- Rich domain modeling: `Card`, `Hand`, `HandEvaluator`, `ShoeManager`, `DealerLogic`, `PayoutService`.
- Session-backed game state serialization (round-trip snapshot with events, settlement results, net delta, GameId).
- Action gating (Hit / Stand / Split / Double / Dealer / Settle) with validation reasons & tooltips.
- Event banner + floating end-of-round summary overlay (auto-hide) showing per-hand outcomes & aggregate net.
- Persistence layer storing game + hand outcomes and aggregate statistics.
- Debug `/Game/Debug/State` page exposing raw JSON snapshot & CSP image probe.
- Tailwind CSS pipeline (build & watch) plus initial CDN fallback.
- Comprehensive unit + integration tests (32 passing) covering evaluator, payouts, dealer flow, settlement, splits, doubles, blackjack edge cases.

## Tech Stack
- **Runtime:** .NET 9 (preview) / ASP.NET Core Razor Pages
- **Data:** EF Core with Sqlite
- **Styling:** Tailwind CSS (via `tailwindcss` npm package) + minimal custom CSS
- **Testing:** xUnit
- **Build CI:** GitHub Actions (Linux + Windows matrix, Tailwind build, perf smoke placeholder)

## Getting Started
### Prerequisites
- .NET 9 SDK (ensure preview installed)  
- Node.js 18+ (for Tailwind build)  
- Git & modern browser

### Clone & Restore
```pwsh
git clone https://github.com/your-org/blackjack-deckofcards.git
cd blackjack-deckofcards/BlackjackRazor
dotnet restore
npm ci
```

### Development Build (with CSS watch)
```pwsh
dotnet build
npm run watch:css
```
In a separate terminal:
```pwsh
dotnet run
```
Navigate to `https://localhost:5001` (or shown port). Sign in with a username to initialize session.

### Tailwind Production CSS
```pwsh
npm run build:css
```
Generated file: `wwwroot/css/site.css`.

### Running Tests
From repo root:
```pwsh
dotnet test BlackjackRazor.Tests/BlackjackRazor.Tests.csproj -c Debug
```

## Game Flow Overview
1. New round (`New`) initializes context; call `Deal` to draw initial hands.
2. Player acts across one or multiple hands (split creates two index-ordered hands; double auto-completes hand).
3. After all player hands complete, trigger dealer play (`Dealer`).
4. Round enters Settled phase; `Settle` computes payouts, updates bankroll, writes persistence.
5. Overlay summary appears with net delta and hand outcomes; auto-dismisses.

## Domain Components
| Component | Responsibility |
|-----------|----------------|
| `HandEvaluator` | Calculates totals, soft/hard aces, blackjack/bust markers |
| `ShoeManager` | Deterministic draw & depletion logic; integrates external deck API (future) |
| `DealerLogic` | Draws according to `HitSoft17` config; produces transcript |
| `PayoutService` | Applies blackjack (3:2), win/lose/push, split rules, doubled bet adjustments |
| `GameService` | Orchestrates lifecycle, events, snapshots, settlement list & net delta |
| `GameRoundPersister` | Persists game and hand outcomes, aggregates stats |

## Accessibility (Planned)
Upcoming improvements: ARIA roles for overlays, focus management on summary banner, live regions for event updates, alt text for card images.

## Performance Harness (Planned)
A simulation runner will execute thousands of rounds headless to flag memory growth & timing outliers; future CI integration using dedicated job.

## CI Pipeline
Workflow file: `.github/workflows/ci.yml` includes:
- Restore, build, test on Ubuntu (primary gate)
- Windows smoke build
- Tailwind conditional build
- Placeholder perf smoke step (will hook simulation harness later)
Artifacts: test results (TRX) uploaded for PR diagnostics.

## Configuration Flags
`BlackjackOptions`:
- `DefaultDeckCount` (current default: 1)
- `DealerHitSoft17` (draw on soft 17 when true)

## Roadmap / Remaining Tasks
- Image visibility test (Playwright / bUnit) to validate hole card reveal
- Responsive mobile tuning (card scaling + layout wrapping)
- Accessibility & ARIA enhancements
- Simulation harness & perf metrics
- Coverage traceability document & final acceptance checklist

## Contributing
1. Fork & branch (`feature/<short-name>`).  
2. Ensure all tests pass locally.  
3. Submit PR; CI must be green.  
4. Include description of user-facing changes plus any domain rule adjustments.

## License
MIT - see `LICENSE`.

## Troubleshooting
| Issue | Fix |
|-------|-----|
| Dealer doesn’t play | Ensure all player hands completed; click Dealer action after stands/doubles |
| Split button disabled | Check ranks equal & exactly two cards; cannot re-split derived child hand |
| Double ignored | Hand must have exactly two cards and not previously doubled |
| CSS not updating | Confirm `npm run watch:css` running; fallback CDN still active |

## Acknowledgments
Built as a structured exploration of deterministic game mechanics, state persistence, and rich UI feedback in Razor Pages.
