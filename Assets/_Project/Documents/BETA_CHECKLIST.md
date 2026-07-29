# Beta Release Checklist: Game Loop

This document tracks the remaining tasks required to complete the core game loop for the beta release. 

## 🚧 Incomplete (Wiring & Polish)
These systems are built but require final wiring, UI connections, or polish.

- [x] **Step 3: Text Editor UI Polish**
  - Ensure the `TextEditorSystem` UI is visually complete and functionally smooth for the player to edit documents. (Completed: Refactored UXML styles, resolved UI Toolkit pointer capture bugs, fixed multi-select persistence, and ensured stable caret focus).
- [x] **Step 4: Grading Failure Feedback Loop**
  - In `FormatDataLoader.cs` (`EvaluatePrintJob`), replace the `TODO` with actual logic to display `result.adviserFeedback[0]` in the Adviser dialogue box so the player knows why their print failed. (Completed: Implemented Clippy UI Toolkit connection)
- [x] **Step 4: Grading Success/Print Polish**
  - In `FormatDataLoader.cs` (`EvaluatePrintJob`), replace the `TODO` for perfect prints. Wire up the success sound, grant XP (if applicable), and trigger the final success UI. (Completed: Built cross-scene GameManager mailbox architecture, implemented auto-collection script, and wired dynamic Dialogue System injection).

## ❌ Not Yet (Needs to be built)
These elements are missing and must be constructed from scratch for the beta flow.

- [ ] **Step 1: Day 1 Intro Cutscene**
  - Build the timeline/sequence for the player spawning into the campus.
  - Implement player movement locking during the sequence.
  - Deliver the introductory context/dialogue before the player gains control.
- [x] **Step 5: Campus Exit Trigger (Adapted for Beta)**
  - Updated `SceneGateInstance` with a Trigger Mode enum (`Interact` or `OnEnter`) to automatically warp the player out of the publication office once the beta concludes.
- [ ] **Step 5: Outro Cutscene**
  - Build the outro timeline/sequence triggered by the campus gate.
  - Ensure `CompleteLevel()` and the final game save are explicitly called at the end of this cutscene to finish the beta demo.
