# Accessibility Audit & Enhancements

## Implemented Improvements
- Card components (`_Card.cshtml`) now expose human-readable labels (e.g., "Ace of Spades") via `aria-label` and are keyboard-focusable (`tabindex="0"`).
- Event banner upgraded with `role="status"` and `aria-live="polite"` plus an assertive hidden live region for screen reader announcement.
- End-of-round summary overlay converted to an accessible dialog (`role="dialog"`, `aria-modal="true"`, labeled by its heading, auto-focus on open, dismiss button labeled).
- Round net & per-hand outcomes remain in a semantic `<table>` with textual cells; color-only indicators supplemented by textual numeric values.

## Pending / Recommended Next Steps
1. Focus Trap: Implement keyboard focus containment within dialog while visible (currently only initial focus set).  
2. Escape Key Dismissal: Add key listener to close the summary overlay with `Esc`.  
3. High Contrast Mode: Ensure contrast ratios meet WCAG AA (verify greens/reds on dark background).  
4. Reduced Motion: Respect `prefers-reduced-motion` for fade animation.  
5. Landmarks: Add `role="banner"`, `role="contentinfo"` to header/footer; verify a single `main` landmark.
6. Screen Reader Card Order: Consider `aria-describedby` to indicate hand index for grouped cards.
7. Live Event Queue: Optionally announce only significant events (blackjack, bust, settlement) with `aria-live="assertive"` and throttle repetitive updates.

## Testing Guide
Run through accessibility checks:
- Keyboard Only: Tab through cards and actions, verify dialog takes focus when shown.
- Screen Reader (NVDA/JAWS): Confirm event updates are announced, card labels are read correctly.
- Axe / Wave Scan: Verify no critical violations; address any contrast or landmark issues.

## Future Tracking
Add results of each audit run here with date, tool, and summary of issues fixed.

| Date | Tool | Issues Found | Action Taken |
|------|------|--------------|--------------|
| (pending) | Axe | TBD | TBD |

## Rationale
Accessible UI ensures inclusivity and improves overall usability (focus indicators, clear semantic structure, proper live announcements for dynamic game events).
