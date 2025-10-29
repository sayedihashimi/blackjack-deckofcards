# Test Coverage Traceability

Maps functional requirements (FR) & acceptance criteria (AC) to implemented tests.

| FR / AC | Description | Key Classes | Tests Covering |
|---------|-------------|-------------|----------------|
| FR-01 | Hand evaluation with soft/hard aces | HandEvaluator | HandEvaluatorTests.* |
| FR-02 | Dealer stands on hard 17, configurable soft 17 | DealerLogic | DealerLogicTests.* |
| FR-03 | Shoe multi-deck draw & exhaustion handling | ShoeManager | ShoeManagerTests.* |
| FR-04 | Payout rules (blackjack 3:2, pushes, bust, split 21) | PayoutService | PayoutServiceTests.* |
| FR-05 | Game flow state transitions (NotStarted→PlayerActing→DealerActing→Settled) | GameService | GameServiceTests.FullRound_* , GameServiceSettlementTests.Settlement_* |
| FR-06 | Blackjack auto-complete & event logging | GameService | GameServiceSettlementTests.NaturalBlackjack_* , GameFlowIntegrationTests.BlackjackVsDealerNonBlackjack_* |
| FR-07 | Split logic and per-hand indexing | GameService | GameServiceTests.SplitPairCreatesTwoHands, GameServiceSettlementTests.Split_RecordsEvent*, GameFlowIntegrationTests.SplitAndDoubleFlow_* |
| FR-08 | Double down rules | GameService | GameServiceSettlementTests.Double_InvalidWhenMoreThanTwoCards_*, GameFlowIntegrationTests.SplitAndDoubleFlow_* |
| FR-09 | Dealer bust detection & event | DealerLogic/GameService | GameServiceSettlementTests.DealerBust_EventRecorded, GameFlowIntegrationTests.DealerBustAfterPlayerStand_* |
| FR-10 | Settlement net delta & bankroll update | GameService/PayoutService | GameServiceSettlementTests.Settlement_ProducesResults*, Integration tests settlement assertions |
| FR-11 | Snapshot serialization & round-trip | GameStateSerializer | GameStateSerializerTests.* |
| FR-12 | End-of-round overlay summary presence | UI RoundSummary | (Manual/UI) pending (future Playwright) |
| FR-13 | Hidden dealer hole card until dealer turn | Play UI logic | HiddenCardVisibilityTests.DealerHoleCardHiddenUntilDealerPhase |
| FR-14 | Performance stability (bulk rounds) | PerformanceSimulator | PerformanceSimulationTests.Run_2000_Rounds_Fast (skipped) |
| FR-15 | Accessibility event announcements | Play page ARIA | (Manual axe scan) documented in accessibility.md |

## Coverage Gaps / Future Tests
- UI overlay (RoundSummary) rendering & dismissal (Playwright/bUnit).
- Focus trap & ESC key for dialog (not yet implemented).
- Split + multiple doubles multi-round sequence (compound bankroll evolution).
- Stress test of deck refresh after depletion.

## Metrics (Qualitative)
Current automated tests emphasize domain logic purity and deterministic outcomes. Integration tests provide scenario coverage for payout permutations. UI accessibility and animation aspects pending automated browser validation.

## Update Procedure
When adding a feature, append a new FR row and reference at least one automated test or a planned manual test placeholder.
