# Final Acceptance Checklist

## Functional
- [x] New game initialization & deal sequencing
- [x] Player actions (Hit / Stand / Split / Double) validated & gated
- [x] Dealer logic honors HitSoft17 flag (current: false)
- [x] Settlement computes correct payouts & updates bankroll
- [x] Events logged for blackjack, bust, stand, split, double, dealer outcome, settlement
- [x] Hidden dealer hole card until dealer phase

## Persistence
- [x] Game & hand outcomes persisted (GameId attached post-create)
- [x] Aggregate stats fields incremented (wins, busts, blackjacks, pushes)

## UI / UX
- [x] Responsive layout (cards scale on narrow widths)
- [x] End-of-round summary overlay (auto-hide + dismiss)
- [x] Tooltips / reasons for disabled action buttons
- [x] Debug state page for snapshot inspection

## Accessibility
- [x] Human-readable card labels (aria-label)
- [x] Live region for events (status + assertive hidden region)
- [x] Dialog semantics on round summary overlay
- [ ] Focus trap in dialog (future)
- [ ] Escape key closes dialog (future)

## Testing
- [x] Unit tests (evaluator, dealer, payouts, shoe)
- [x] Integration scenario tests (split/double/bust/push/blackjack)
- [x] Hidden card visibility test
- [x] Performance harness (manual, skipped in CI)
- [ ] UI automation test (image reveal & overlay) pending

## Documentation
- [x] README with setup & flow
- [x] Accessibility report (`docs/accessibility.md`)
- [x] Coverage traceability (`docs/testing/traceability.md`)

## CI / DevOps
- [x] GitHub Actions build & test workflow
- [x] Tailwind CSS build in pipeline
- [ ] Code coverage publishing (optional enhancement)
- [ ] Add perf harness job invocation (beyond placeholder)

## Quality Gates
- [x] Builds clean (no errors) on Debug & Release
- [x] Tests pass (non-skipped)
- [ ] Address analyzer warnings (low priority)

## Risk / Deferred Items
| Risk | Mitigation | Status |
|------|------------|--------|
| Lack of automated UI coverage | Add Playwright suite | Pending |
| No focus trap in dialog | Implement JS trap util | Pending |
| Dealer API integration (real deck) untested in perf harness | Mock/local shoe used | Acceptable for now |

## Exit Criteria Summary
Minimum functional, persistence, and settlement logic met. Remaining improvements are UX polish & additional automation which are deferred but tracked. Project ready for initial user evaluation.
