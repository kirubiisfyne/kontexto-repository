<!--
Sync Impact Report:
- Version change: 0.0.0 -> 1.0.0
- Modified Principles:
  - [PRINCIPLE_1_NAME] -> I. Pure & Isolated Architecture ("Dumb" Systems)
  - [PRINCIPLE_2_NAME] -> II. Designer-Centric Workflow (Inspector First)
  - [PRINCIPLE_3_NAME] -> III. Predictability & Robust State Management
  - [PRINCIPLE_4_NAME] -> IV. Reliable Data Persistence & Load Ordering
  - [PRINCIPLE_5_NAME] -> V. Modular & Self-Contained Components
- Added sections: Architectural Constraints, Development Workflow
- Removed sections: N/A
- Templates requiring updates: 
  - .specify/templates/plan-template.md (⚠ pending)
  - .specify/templates/spec-template.md (⚠ pending)
  - .specify/templates/tasks-template.md (⚠ pending)
- Follow-up TODOs: None
-->
# Kontexto Constitution

## Core Principles

### I. Pure & Isolated Architecture ("Dumb" Systems)
Systems MUST NOT depend on or directly reference each other in code. 
- All cross-system logic (e.g., task completions triggering dialogue changes) MUST be decoupled and wired in the Unity Inspector via `UnityEvent`.
- This ensures maximum portability, prevents cascading failures, and adheres to the Single Responsibility Principle.

### II. Designer-Centric Workflow (Inspector First)
Empower non-technical team members to "program" game flow without writing code.
- Provide explicit, parameterless public methods (e.g., `SaveGame()`, `CompleteTask()`) designed specifically for `UnityEvent` wiring.
- Expose clear, self-documenting per-status events (e.g., `onActive`, `onReadyToComplete`) rather than relying on complex branching logic in the Inspector.
- Keep the Inspector clean with `[Serializable]` groups, `[Header]`, and `[Tooltip]`.

### III. Predictability & Robust State Management
Systems MUST prioritize stability and predictability over late-bound flexibility.
- Use explicit state transitions (e.g., `ReadyToComplete` distinct from `Active` or `Completed`).
- Enforce immediate snapshots for critical interactions (e.g., Dialogue snapshot on input).
- Rely on guard clauses in the Inspector and runtime code to prevent invalid states. Handle timing issues or race conditions via deferred coroutines where appropriate, ensuring correct execution order.

### IV. Reliable Data Persistence & Scene-Local Execution
Save and load logic MUST be reliable, explicit, and avoid global singletons where possible.
- Use Scene-local managers (like `LevelLoader` in `Awake()`) instead of Singletons to guarantee precise initialization ordering (spawning prefabs before core scripts run).
- Save crucial state (task completions, player position) using stable identifiers (e.g., `taskId`) to local JSON files.
- On restore, replay the full status chain to fire all intermediate Inspector-wired events, ensuring correct scene setup.

### V. Modular & Self-Contained Components
Avoid splitting systems unnecessarily if it introduces coupling.
- When an entity acts in multiple roles (e.g., an NPC that is both Giver and Closer), extend the existing system (e.g., `HostType.Both`) to handle the full lifecycle internally rather than splitting into multiple components that need to communicate.

## Architectural Constraints

- **Unity Events over Hard References:** Never use `GetComponent` or direct reference calls across distinct systems (like Dialogue and Task). Use Unity Events.
- **Save Hooks:** Use `SaveManager` for pure JSON I/O, without MonoBehaviour lifecycle coupling. Route all save calls through unified endpoints to ensure consistency.
- **Unscaled UI:** UI Systems must support instant pause (`Time.timeScale = 0`) using unscaled time animators, prioritizing player safety.

## Development Workflow

- All new features MUST be built to fit seamlessly into the Unity Inspector workflow.
- Code should be written assuming that prefabs might be instanced dynamically. Prefabs should not hold hard references to scene objects; use bridging scripts (e.g., `SaveGameBridge`) or scene-local managers.
- All significant code changes, feature implementations, or architectural updates MUST be documented in `Assets/Master/Docs/DEVELOPMENT_DIARY.md` prior to committing changes. This ensures the historical archive is always up-to-date with the codebase.

## Governance

- The Constitution supersedes all other coding practices.
- All new features must comply with the isolated architecture rules outlined above.
- Amendments require documentation via the DEVELOPMENT_DIARY and subsequent update to this constitution.

**Version**: 1.0.0 | **Ratified**: 2026-07-07 | **Last Amended**: 2026-07-07
