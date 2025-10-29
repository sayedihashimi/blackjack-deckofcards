# BlackjackRazor Specification

Version: 0.1 (Draft)
Owner: (TBD)
Last Updated: 2025-10-27
Status: Draft

---
## 1. Purpose
Deliver a fast, frictionless, rules-accurate, browser-based Blackjack experience using Razor (ASP.NET Core) with authentic card imagery and persistent per-username session & historical statistics.

## 2. Scope
IN SCOPE:
- Single-player vs dealer Blackjack following Bicycle rules (standard 52-card decks, multiple deck shoe). 
- Actions: Deal, Hit, Stand, Split (only identical rank first two cards), Double Down (standard constraints), (Implicit) Auto-resolve dealer.
- Bets and payout handling (Blackjack 3:2, normal win 1:1, push return bet, double = double stake, split = separate hands each stake, handle split blackjack as 21 not natural blackjack payout).
- Username-only sign-in (no password) with persistent stats keyed by username.
- Responsive dark casino-themed UI matching `assets/blackjack-mockup.png` including dealer row, player hands, action panel, round summary, bankroll, and stats.
- Multiple hands after split with independent bet, outcome, and action enablement.
- Tracking session stats (in-memory) + historical stats (persistent store, e.g. Lite DB / SQLite / file JSON pluggable repository).
- Initial configuration: number of decks (default 6), optional default bet.
- Integration with external Deck of Cards image URLs (no local card asset duplication; leverage remote images for face-up cards; hidden card uses a consistent back design).

OUT OF SCOPE (v1):
- Multiplayer tables / concurrency.
- Advanced side bets (insurance, surrender, even money).
- Chips drag-and-drop UI; use numeric bet input/spinner.
- Authentication security hardening (passwords, OAuth) beyond username uniqueness.
- Localization (English only initially).

## 3. Definitions
- Shoe: Combined shuffled decks (N decks, default 6). Reshuffle trigger when penetration threshold reached (e.g., when remaining < 25%).
- Natural Blackjack: First two cards = Ace + 10-value card. Payout 3:2 unless dealer also has Blackjack (push).
- Soft Hand: A hand containing an Ace counted as 11 without bust.
- Hard Hand: A total that either has no Ace or all Aces counted as 1.

## 4. Functional Requirements
### 4.1 User Onboarding
FR-01: User enters a username (min length 2, max 24, alphanumeric + underscore) and clicks Start to establish session.
FR-02: If username exists historically, load cumulative statistics; else create new record.

### 4.2 Game Setup
FR-03: User can select number of decks (1–8, default 6) before first deal; locked once first hand is dealt (until app reset/new game).
FR-04: User can define a default bet (≥1 and ≤ configurable table max, default 100). Bet is pre-filled each hand and can be edited before dealing.
FR-05: Starting bankroll (configurable default e.g. 5,000). Bankroll persists within session; historical storage may keep lifetime profit separately.

### 4.3 Dealing & Round Flow
FR-06: Deal action consumes current bet (subtracts from available bankroll into wager pot(s)).
FR-07: Initial deal: Player gets two face-up; Dealer gets one face-up, one face-down.
FR-08: If player or dealer has natural Blackjack, resolve per rules (check push / player win) without further actions.
FR-09: While active, actions enabled contextually (see State Machine).
FR-10: Split allowed only if exactly two cards of identical rank and player bankroll can cover second bet.
FR-11: Double Down allowed only on the first action of an unsplit hand (i.e., exactly two initial cards) and bankroll has remaining funds ≥ current hand bet.
FR-12: After Stand / Bust / Double Down resolution (card draw + auto-stand), move focus to next unfinished hand; once all player hands resolved, reveal dealer hole card and auto-play dealer.
FR-13: Dealer hits until rules threshold: Hit on 16 or less; stand on hard 17; configurable for soft 17 (default: dealer stands on soft 17 – confirm Bicycle rules; if Bicycle hits S17 adjust). Document configuration flag.
FR-14: Show dealer draws one at a time with short delay (e.g., 400–600ms) for UX clarity.
FR-15: After final dealer total, settle all hands (win, lose, push, blackjack, bust) and adjust bankroll.
FR-16: Display round summary: list each hand => result type, bet, payout delta, new bankroll.
FR-17: User can begin next round by adjusting bet(s) (base bet reused) and clicking Deal again.

### 4.4 Payout & Accounting
FR-18: Blackjack pays 3:2 (e.g., bet 100 => net +150) unless dealer also Blackjack (push => 0 change).
FR-19: Win pays 1:1 (net +bet). Loss deducts bet (already removed on staking, so no further change). Push returns bet (refund stake to bankroll).
FR-20: Double Down: stake doubled prior to receiving exactly one more card; then result resolves as above.
FR-21: Split: create a second hand duplicating original bet; each hand resolves independently. A split Ace + 10 counts as 21 (not natural Blackjack) for payout 1:1.
FR-22: Track per-hand delta and aggregate round delta.

### 4.5 Statistics
FR-23: Session stats tracked in memory: hands played, wins, losses, pushes, blackjacks (natural only), busts (player bust count), total wagered, net profit.
FR-24: Historical stats (persisted): Aggregate sums per username for above plus last played timestamp and streak counters (optional enhancement placeholder).
FR-25: Stats update atomically after each round resolution.
FR-26: Provide computed win rate (wins / (wins + losses)) and blackjack rate.

### 4.6 Persistence
FR-27: Implement repository abstraction (IStatsRepository) with default local provider (e.g., JSON file per username or a single DB file). Must be swappable for future DB.
FR-28: Race conditions avoided by locking on username file during write.

### 4.7 UI & Interaction
FR-29: Layout matches mockup: Top dealer row (cards horizontal). Under that, player hands in a flex/wrap container. Action controls panel anchored/sticky near bottom or right depending on width.
FR-30: Responsive breakpoints: > 900px (desktop), 600–900 (tablet), < 600 (mobile stacked). Mobile ensures no vertical scroll to see player cards + buttons (target height < 100vh minus safe areas).
FR-31: Each split hand annotated (Hand 1, Hand 2, etc.) with bet amount and status text (e.g., “Busted”, “21”, “Standing”, “Win +100”, “Push”).
FR-32: Disabled actions visually distinct (reduced opacity + disabled pointer events) with accessible aria-disabled attributes.
FR-33: Show bankroll prominently and update with subtle animation (e.g., green flash for gain, red for loss).
FR-34: Use semantic HTML (buttons, lists) and ARIA labels for hidden card and revealed transitions.
FR-35: Ensure hidden dealer hole card shown as card back until reveal.

### 4.8 Error & Edge Handling
FR-36: If shoe lacks sufficient cards for immediate next deal sequence, reshuffle automatically (log event in UI). Existing round always completes using remaining cards (only reshuffle between rounds unless impossible mid-round under extreme card exhaustion).
FR-37: Prevent actions during dealer autoplay.
FR-38: Handle network failure for image loads by showing fallback SVG placeholder with rank/suit text.

### 4.9 Configuration
FR-39: Configurable JSON/Options: defaultDeckCount, defaultStartingBankroll, minBet, maxBet, dealerHitSoft17 flag, shoePenetrationThreshold (% remaining before reshuffle), dealingAnimationDelayMs.

## 5. Non-Functional Requirements
NFR-01: First playable hand within < 5 seconds on median desktop (cold load) and < 8 seconds on typical mobile.
NFR-02: Core interactions (Hit/Stand) apply within < 150ms from click to card render (excluding intentional animation delay).
NFR-03: Page Lighthouse Performance score ≥ 85 (desktop) for production build.
NFR-04: Accessibility: Contrast ratios ≥ WCAG AA for text & controls; focus outline visible; cards have alt text with rank + suit (e.g., “Queen of Hearts”).
NFR-05: No blocking script > 250KB uncompressed; defer non-critical scripts.
NFR-06: All logic (hand evaluation, payouts) covered by ≥ 95% statement coverage; UI components ≥ 70%.
NFR-07: Persistent storage operations complete < 50ms average (local mode) for stat save.

## 6. Domain Model
Entities:
- Card { Rank (2–10,J,Q,K,A), Suit (♠,♥,♦,♣), Value(s) (1/11 for Ace, numeric for others) }
- Hand { Id, Cards[], BetAmount, IsSplitChild, IsCompleted, HasDoubled, ResolvedResult }
- Round { Id, PlayerHands[], DealerHand, OutcomeSummary[], NetDelta }
- Stats { Username, Hands, Wins, Losses, Pushes, Blackjacks, Busts, TotalWagered, NetProfit, CreatedAt, UpdatedAt }
- Shoe { Cards[], DeckCount, RemainingCount, NeedsReshuffle() }

## 7. State Machine (Per Round)
States: Idle → Dealt → PlayerTurn (activeHand pointer) → DealerReveal → DealerPlay → Settlement → Summary → Idle (next round)
Actions per state:
- Idle: Deal (enabled), others disabled.
- Dealt/PlayerTurn (hand-level context): Hit (unless total >= 21), Stand (always), Double (if exactly 2 cards & not split child after a hit), Split (if exactly 2 ranks equal & bankroll covers), (Auto-advance on bust or 21).
- DealerReveal: No player actions.
- DealerPlay: No player actions.
- Settlement: No actions until summary displayed.
- Summary: Deal (next round) enabled once stats updated.

## 8. Action Enablement Matrix (Simplified)
| Action | Condition |
|--------|-----------|
| Deal | State=Idle & bet valid & bankroll >= bet |
| Hit | ActiveHand & total < 21 |
| Stand | ActiveHand |
| Double | ActiveHand & cards==2 & !HasDoubled & bankroll >= bet & not already split-continued (per rule) |
| Split | ActiveHand & cards==2 & ranks equal & bankroll >= bet & splitsRemaining < configuredMax (default maybe 3) |

## 9. Algorithms
Hand Value Evaluation:
1. Sum all non-aces.
2. Count aces; add 11 for first ace if sum + 11 + (acesRemaining-1) ≤ 21 else add 1; remaining aces add 1 each.
3. Track soft/hard flag.

Dealer Play:
Loop while (total < 17) OR (total == 17 AND soft AND dealerHitSoft17 == true).

## 10. Data Persistence Strategy
Interface IStatsRepository:
- Task<Stats?> GetAsync(username)
- Task UpsertAsync(Stats stats)
- Task<bool> ExistsAsync(username)
Implementation v1: FileStatsRepository (JSON file per username: /data/users/{username}.json). Future: EF Core / SQLite.
Concurrency: File lock (open with share none) during write.

## 11. External Integrations
Card Images: Use Deck of Cards API pattern (e.g., https://deckofcardsapi.com/static/img/{code}.png or equivalent). Map ranks: 0 = 10 (T), Face letters consistent. Provide adapter function CardToImageUrl(Card). Hidden card uses local `/img/card-back.png` or remote generic back.

## 12. Security & Privacy
- Username stored in plain text; no authentication guarantees.
- No PII collected beyond username.
- CSRF minimal risk (single-page interactions); still use anti-forgery token if posting forms.

## 13. Performance Considerations
- Pre-generate and shuffle shoe using Fisher-Yates.
- Avoid re-rendering entire page; componentize hands.
- Lazy-load stats panel if needed.

## 14. Accessibility
- All action buttons: aria-label and disabled states via aria-disabled.
- Live region for dealer action announcements (e.g., “Dealer hits: 18”).
- Provide text summary after round (role=alert or aria-live polite).

## 15. Error Handling
- Display inline toast for exceptional errors (shoe corruption, repository issue) with retry suggestion.
- Fail-safe: If persistence fails, continue gameplay but warn that stats may not save.

## 16. Logging & Telemetry (Optional v1 Hook)
- Console logging for state transitions in dev.
- Abstract ILogger for production injection.

## 17. Testing Strategy
Unit Tests:
- Hand value calculations (all ace permutations).
- Split logic (multiple splits where allowed edge cases).
- Double down constraints.
- Dealer play edge cases (soft 17 behaviors).
- Payout scenarios (blackjack vs push vs split 21).
Integration Tests:
- Full round flows (normal win, dealer bust, player bust, push, blackjack tie, split then outcomes).
- Stats accumulation.
UI/Component Tests (if using bUnit / Playwright):
- Action enablement toggles.
- Mobile layout snapshot.
Performance Tests:
- Simulate 10k rounds headless to validate stat accumulation speed & no memory leak.
Accessibility Tests:
- Axe scan on primary page.

## 18. Acceptance Criteria Mapping
| ID | Success Criterion | Verification |
|----|-------------------|--------------|
| AC1 | Start playing in <5s | Manual + Lighthouse TTI |
| AC2 | Dealer hole card hidden initially | UI test screenshot diff |
| AC3 | Blackjack payout 3:2 accurate | Unit test payout_blackjack() |
| AC4 | Split creates independent hands with independent bets | Integration test |
| AC5 | Double allowed only on first action with 2 cards | Unit + UI test |
| AC6 | Stats accurate after 100 simulated rounds | Simulation test |
| AC7 | Mobile no-scroll primary actions visible | Responsive test |
| AC8 | Dealer soft 17 behavior matches config | Unit test dealer_soft17() |
| AC9 | Round summary shows per-hand deltas | UI test |
| AC10 | Card images load from external source | Network inspection test |

## 19. Implementation Roadmap
Phase 1 (Foundation): Models, shoe shuffle, hand evaluator, repository abstraction, unit tests core logic.
Phase 2 (Gameplay Core): Razor pages/components for table, actions, state machine, dealer automation.
Phase 3 (Betting & Payout): Bankroll, bets, split & double flows, payout resolution, stats updates.
Phase 4 (UI Polish & Responsive): Theming, animations, accessibility, mobile refinement.
Phase 5 (Persistence & History): File-based stats retrieval, historical aggregation view.
Phase 6 (Testing & Hardening): Coverage push, performance simulation, acceptance matrix completion.
Phase 7 (Optimization & Optional Telemetry): Minor perf tweaks, error instrumentation.

## 20. Open Questions / Decisions (Track & Resolve)
- Confirm Bicycle rule: Dealer stands or hits on soft 17? (Assumed stands. If contrary, flip config default.)
- Maximum number of splits allowed? (Commonly up to 3 splits, Aces often only one card after split; implement config: maxSplits=3, splitAcesDrawOne=true.)
- Insurance / Surrender intentionally excluded; document rationale in README.

## 21. Future Enhancements (Parking Lot)
- Add insurance & surrender options.
- Add basic strategy hints overlay.
- Animated chip betting UI.
- Progressive achievements & streak tracking.
- Cloud sync / multi-device.

## 22. Traceability
Each FR maps to at least one test case before release (maintain traceability matrix in /docs/testing/traceability.md later).

## 23. Risks & Mitigations
| Risk | Impact | Mitigation |
|------|--------|-----------|
| Misapplied dealer rule (soft 17) | Incorrect outcomes | Config + explicit unit tests |
| Card image host downtime | Broken visual UX | Fallback SVG rank/suit placeholder |
| Stats file corruption | Loss of history | Write temp then atomic rename |
| Performance regressions | Slow UX | Add perf test harness simulation |

## 24. Approval
Sign-off roles (TBD): Product, Engineering, QA.

---
End of Specification.
