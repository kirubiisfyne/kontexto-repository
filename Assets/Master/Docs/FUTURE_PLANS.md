# Future Plans & Technical Roadmap

This document serves as a holding area for architectural improvements, refactoring ideas, and technical debt that are planned for implementation *after* the current release cycle.

---

## 1. The Orchestrator / Bridge Pattern
**Target Timing**: Post-Beta Release
**Context**: Our current architecture relies heavily on `UnityEvents` for cross-system communication (e.g., Task System triggering Dialogue System updates). While this perfectly isolates systems, it creates "spaghetti wiring" in the Inspector, making the game's flow difficult to trace and debug at runtime.

### Proposed Solution
Introduce the **Mediator Pattern** via "Orchestrator" scripts to move mandatory systemic wiring from the Inspector into C#.

*   **Implementation**: Create bridge scripts (e.g., `TaskDialogueBridge.cs`) that sit on the NPC GameObject. 
*   **Behavior**: The bridge script subscribes to the C# events of the Task manager (`OnTaskActive`, `OnTaskCompleted`) and calls the corresponding methods on the Dialogue manager.
*   **Architectural Integrity**: `HostTaskManager` and `DialogueManager` remain 100% isolated and "dumb." They still do not know about each other. Only the Bridge knows about both.
*   **Benefits**: 
    *   Grants compile-time safety (broken references trigger IDE errors).
    *   Enables standard C# debugging (breakpoints can be placed in the bridge).
    *   Cleans up the Inspector massively.
    *   `UnityEvents` are then reserved exclusively for level-specific, cosmetic flair (audio, VFX).
